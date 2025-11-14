import { inject, Injectable, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { firstValueFrom, Observable, Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

import {
  AlertDialogConfig,
  ConfirmDialogConfig,
  ConfirmDialogResult,
  CustomDialogConfig,
  DEFAULT_DIALOG_CONFIG,
  DIALOG_PRESETS,
  PromptDialogConfig,
  PromptDialogResult,
  LoadingDialogConfig,
} from '../models/dialog.models';

/**
 * ═══════════════════════════════════════════════════════════════════════
 * DIALOG SERVICE - FIXED
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * FIXES:
 * ✅ setInterval cleanup handle stored
 * ✅ Cleaned up in destroyRef.onDestroy
 * ✅ No memory leaks from interval timer
 */

interface DialogTracker {
  hash: string;
  ref: MatDialogRef<any>;
  timestamp: number;
}

@Injectable({
  providedIn: 'root',
})
export class DialogService {
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  // ═══════════════════════════════════════════════════════════════════════
  // State Management
  // ═══════════════════════════════════════════════════════════════════════
  
  private readonly MAX_DIALOGS = 3;
  private readonly DEDUP_WINDOW_MS = 500;
  
  private readonly recentDialogs = new Map<string, DialogTracker>();
  private readonly dialogQueue: Array<() => Promise<any>> = [];
  private readonly _isProcessingQueue = signal(false);
  private readonly _loadingDialogRef = signal<MatDialogRef<any> | null>(null);

  // FIX: Store cleanup interval handle
  private cleanupIntervalHandle?: ReturnType<typeof setInterval>;

  readonly isProcessingQueue = this._isProcessingQueue.asReadonly();

  constructor() {
    this.setupCleanup();
    
    // FIX: Register cleanup on destroy
    this.destroyRef.onDestroy(() => {
      if (this.cleanupIntervalHandle) {
        clearInterval(this.cleanupIntervalHandle);
        this.cleanupIntervalHandle = undefined;
      }
    });
  }

  // ═══════════════════════════════════════════════════════════════════════
  // Core Dialog Methods
  // ═══════════════════════════════════════════════════════════════════════

  async confirm(config: ConfirmDialogConfig | string): Promise<boolean> {
    const dialogConfig = this.normalizeConfirmConfig(config);
    const hash = this.createHash('confirm', dialogConfig);

    if (this.isDuplicate(hash)) {
      return false;
    }

    if (this.hasReachedMaxDialogs()) {
      console.warn('[DialogService] Maximum dialogs reached. Dialog queued.');
      return this.queueDialog(() => this.confirm(config));
    }

    const { ConfirmDialogComponent } = await import(
      '../components/confirm-dialog.component'
    );

    const dialogRef = this.dialog.open<any, ConfirmDialogConfig, ConfirmDialogResult>(
      ConfirmDialogComponent,
      {
        ...DEFAULT_DIALOG_CONFIG,
        width: dialogConfig.width || DEFAULT_DIALOG_CONFIG.width,
        disableClose: dialogConfig.disableClose ?? DEFAULT_DIALOG_CONFIG.disableClose,
        data: dialogConfig,
      }
    );

    this.trackDialog(hash, dialogRef);

    const result = await firstValueFrom(dialogRef.afterClosed());
    this.removeFromTracker(hash);
    
    return result?.confirmed ?? false;
  }

  async alert(config: AlertDialogConfig | string): Promise<void> {
    const dialogConfig = this.normalizeAlertConfig(config);
    const hash = this.createHash('alert', dialogConfig);

    if (this.isDuplicate(hash)) {
      return;
    }

    if (this.hasReachedMaxDialogs()) {
      console.warn('[DialogService] Maximum dialogs reached. Dialog queued.');
      return this.queueDialog(() => this.alert(config));
    }

    const { AlertDialogComponent } = await import(
      '../components/alert-dialog.component'
    );

    const dialogRef = this.dialog.open<any, AlertDialogConfig, void>(
      AlertDialogComponent,
      {
        ...DEFAULT_DIALOG_CONFIG,
        width: dialogConfig.width || DEFAULT_DIALOG_CONFIG.width,
        disableClose: dialogConfig.disableClose ?? DEFAULT_DIALOG_CONFIG.disableClose,
        data: dialogConfig,
      }
    );

    this.trackDialog(hash, dialogRef);

    await firstValueFrom(dialogRef.afterClosed());
    this.removeFromTracker(hash);
  }

  async prompt(config: PromptDialogConfig | string): Promise<string | null> {
    const dialogConfig = this.normalizePromptConfig(config);
    const hash = this.createHash('prompt', dialogConfig);

    if (this.isDuplicate(hash)) {
      return null;
    }

    if (this.hasReachedMaxDialogs()) {
      console.warn('[DialogService] Maximum dialogs reached. Dialog queued.');
      return this.queueDialog(() => this.prompt(config));
    }

    const { PromptDialogComponent } = await import(
      '../components/prompt-dialog.component'
    );

    const dialogRef = this.dialog.open<any, PromptDialogConfig, PromptDialogResult>(
      PromptDialogComponent,
      {
        ...DEFAULT_DIALOG_CONFIG,
        width: dialogConfig.width || DEFAULT_DIALOG_CONFIG.width,
        disableClose: dialogConfig.disableClose ?? DEFAULT_DIALOG_CONFIG.disableClose,
        data: dialogConfig,
      }
    );

    this.trackDialog(hash, dialogRef);

    const result = await firstValueFrom(dialogRef.afterClosed());
    this.removeFromTracker(hash);
    
    return result?.submitted ? (result.value ?? null) : null;
  }

  async showLoading(config: LoadingDialogConfig | string): Promise<MatDialogRef<any>> {
    const dialogConfig = typeof config === 'string' 
      ? { message: config } 
      : config;

    const { LoadingDialogComponent } = await import(
      '../components/loading-dialog.component'
    );

    const dialogRef = this.dialog.open(
      LoadingDialogComponent,
      {
        ...DEFAULT_DIALOG_CONFIG,
        width: '300px',
        disableClose: true,
        data: dialogConfig,
      }
    );

    this._loadingDialogRef.set(dialogRef);
    return dialogRef;
  }

  updateLoading(message?: string, progress?: number): void {
    const ref = this._loadingDialogRef();
    if (ref?.componentInstance) {
      if (message !== undefined) {
        ref.componentInstance.message = message;
      }
      if (progress !== undefined) {
        ref.componentInstance.progress = progress;
      }
    }
  }

  hideLoading(): void {
    const ref = this._loadingDialogRef();
    if (ref) {
      ref.close();
      this._loadingDialogRef.set(null);
    }
  }

  openCustom<T, R = any>(config: CustomDialogConfig<T>): Observable<R | undefined> {
    if (this.hasReachedMaxDialogs()) {
      console.warn('[DialogService] Maximum dialogs reached.');
    }

    const dialogRef = this.dialog.open<any, T, R>(config.component, {
      ...DEFAULT_DIALOG_CONFIG,
      ...config,
    });

    return dialogRef.afterClosed();
  }

  async openCustomAsync<T, R = any>(config: CustomDialogConfig<T>): Promise<R | undefined> {
    return firstValueFrom(this.openCustom<T, R>(config));
  }

  // ═══════════════════════════════════════════════════════════════════════
  // Semantic Presets
  // ═══════════════════════════════════════════════════════════════════════

  async confirmDelete(itemName?: string): Promise<boolean> {
    return this.confirm({
      ...DIALOG_PRESETS.confirmDelete,
      message: itemName 
        ? `Are you sure you want to delete "${itemName}"? This action cannot be undone.`
        : DIALOG_PRESETS.confirmDelete.message,
    });
  }

  async confirmDiscard(message?: string): Promise<boolean> {
    return this.confirm({
      ...DIALOG_PRESETS.confirmDiscard,
      message: message || DIALOG_PRESETS.confirmDiscard.message,
    });
  }

  async confirmLeave(message?: string): Promise<boolean> {
    return this.confirm({
      ...DIALOG_PRESETS.confirmLeave,
      message: message || DIALOG_PRESETS.confirmLeave.message,
    });
  }

  async confirmLogout(): Promise<boolean> {
    return this.confirm(DIALOG_PRESETS.confirmLogout);
  }

  async alertSuccess(message: string, title?: string): Promise<void> {
    return this.alert({
      ...DIALOG_PRESETS.success,
      message,
      title: title || DIALOG_PRESETS.success.title,
    });
  }

  async alertError(message: string, title?: string): Promise<void> {
    return this.alert({
      ...DIALOG_PRESETS.error,
      message,
      title: title || DIALOG_PRESETS.error.title,
    });
  }

  async alertInfo(message: string, title?: string): Promise<void> {
    return this.alert({
      ...DIALOG_PRESETS.info,
      message,
      title: title || DIALOG_PRESETS.info.title,
    });
  }

  async alertWarning(message: string, title?: string): Promise<void> {
    return this.alert({
      ...DIALOG_PRESETS.warning,
      message,
      title: title || DIALOG_PRESETS.warning.title,
    });
  }

  // ═══════════════════════════════════════════════════════════════════════
  // Dialog Management
  // ═══════════════════════════════════════════════════════════════════════

  closeAll(): void {
    this.dialog.closeAll();
    this.recentDialogs.clear();
    this.dialogQueue.length = 0;
    this._loadingDialogRef.set(null);
  }

  hasOpenDialogs(): boolean {
    return this.dialog.openDialogs.length > 0;
  }

  getOpenDialogCount(): number {
    return this.dialog.openDialogs.length;
  }

  // ═══════════════════════════════════════════════════════════════════════
  // Deduplication & Queue
  // ═══════════════════════════════════════════════════════════════════════

  private createHash(type: string, config: any): string {
    const message = config.message || '';
    const title = config.title || '';
    return `${type}:${title}:${message}`;
  }

  private isDuplicate(hash: string): boolean {
    const recent = this.recentDialogs.get(hash);
    if (!recent) return false;

    const elapsed = Date.now() - recent.timestamp;
    return elapsed < this.DEDUP_WINDOW_MS;
  }

  private trackDialog(hash: string, ref: MatDialogRef<any>): void {
    this.recentDialogs.set(hash, {
      hash,
      ref,
      timestamp: Date.now(),
    });

    ref.afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.removeFromTracker(hash);
        this.processQueue();
      });
  }

  private removeFromTracker(hash: string): void {
    this.recentDialogs.delete(hash);
  }

  private hasReachedMaxDialogs(): boolean {
    return this.dialog.openDialogs.length >= this.MAX_DIALOGS;
  }

  private async queueDialog<T>(dialogFn: () => Promise<T>): Promise<T> {
    return new Promise((resolve, reject) => {
      this.dialogQueue.push(async () => {
        try {
          const result = await dialogFn();
          resolve(result);
          return result;
        } catch (error) {
          reject(error);
          throw error;
        }
      });

      this.processQueue();
    });
  }

  private async processQueue(): Promise<void> {
    if (this._isProcessingQueue() || this.dialogQueue.length === 0) {
      return;
    }

    if (this.hasReachedMaxDialogs()) {
      return;
    }

    this._isProcessingQueue.set(true);

    const nextDialog = this.dialogQueue.shift();
    if (nextDialog) {
      try {
        await nextDialog();
      } catch (error) {
        console.error('[DialogService] Queue processing error:', error);
      }
    }

    this._isProcessingQueue.set(false);

    if (this.dialogQueue.length > 0 && !this.hasReachedMaxDialogs()) {
      setTimeout(() => this.processQueue(), 100);
    }
  }

  // ═══════════════════════════════════════════════════════════════════════
  // Cleanup - FIXED
  // ═══════════════════════════════════════════════════════════════════════

  private setupCleanup(): void {
    this.cleanupIntervalHandle = setInterval(() => {
      const now = Date.now();
      for (const [hash, tracker] of this.recentDialogs.entries()) {
        if (now - tracker.timestamp > this.DEDUP_WINDOW_MS) {
          this.recentDialogs.delete(hash);
        }
      }
    }, this.DEDUP_WINDOW_MS);
  }

  // ═══════════════════════════════════════════════════════════════════════
  // Config Normalizers
  // ═══════════════════════════════════════════════════════════════════════

  private normalizeConfirmConfig(config: ConfirmDialogConfig | string): ConfirmDialogConfig {
    if (typeof config === 'string') {
      return {
        message: config,
        confirmText: 'Confirm',
        cancelText: 'Cancel',
      };
    }
    return {
      confirmText: 'Confirm',
      cancelText: 'Cancel',
      ...config,
    };
  }

  private normalizeAlertConfig(config: AlertDialogConfig | string): AlertDialogConfig {
    if (typeof config === 'string') {
      return {
        message: config,
        okText: 'OK',
      };
    }
    return {
      okText: 'OK',
      ...config,
    };
  }

  private normalizePromptConfig(config: PromptDialogConfig | string): PromptDialogConfig {
    if (typeof config === 'string') {
      return {
        message: config,
        inputType: 'text',
        confirmText: 'Submit',
        cancelText: 'Cancel',
      };
    }
    return {
      inputType: 'text',
      confirmText: 'Submit',
      cancelText: 'Cancel',
      ...config,
    };
  }

  // ═══════════════════════════════════════════════════════════════════════
  // Diagnostics
  // ═══════════════════════════════════════════════════════════════════════

  getDiagnostics() {
    return {
      openDialogs: this.dialog.openDialogs.length,
      queuedDialogs: this.dialogQueue.length,
      recentDialogs: this.recentDialogs.size,
      isProcessingQueue: this._isProcessingQueue(),
      maxDialogs: this.MAX_DIALOGS,
      hasLoadingDialog: !!this._loadingDialogRef(),
      hasCleanupInterval: !!this.cleanupIntervalHandle,
    };
  }
}

import { inject, Injectable, signal, computed } from '@angular/core';
import { MatDialog, MatDialogConfig } from '@angular/material/dialog';
import { firstValueFrom, Observable, Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

import {
  AlertDialogConfig,
  ConfirmDialogConfig,
  ConfirmDialogResult,
  CustomDialogConfig,
  DEFAULT_DIALOG_CONFIG,
  DIALOG_PRESETS,
  FormDialogConfig,
  FormDialogResult,
  PromptDialogConfig,
  PromptDialogResult
} from '../models/dialog.models';

/**
 * Dialog Service - Production Ready
 * 
 * USAGE GUIDELINES:
 * ━━━━━━━━━━━━━━━━━
 * Dialog vs Toast:
 * - Use DIALOGS for: Important decisions, confirmations, forms, critical errors
 * - Use TOASTS for: Success feedback, transient info, background notifications
 * 
 * FEATURES:
 * ━━━━━━━━━
 * ✅ Deduplication (prevents identical dialogs)
 * ✅ Queue Management (sequential dialogs)
 * ✅ Max Limit (prevents dialog spam)
 * ✅ Semantic Presets (confirmDelete, alertSuccess, etc)
 * ✅ Type Safety (strongly typed configs & results)
 * ✅ Lazy Loading (components loaded on demand)
 * ✅ Promise-based API (async/await friendly)
 */

@Injectable({
  providedIn: 'root'
})
export class DialogService {
  private readonly dialog = inject(MatDialog);
  private readonly destroy$ = new Subject<void>();

  // Configuration
  private readonly MAX_DIALOGS = 3;
  private readonly DEDUP_WINDOW_MS = 1000;

  // State
  private readonly _activeDialogs = signal<string[]>([]);
  private readonly _queuedDialogs = signal<QueuedDialog[]>([]);
  private readonly recentDialogs = new Map<string, number>();

  // Public state
  readonly activeDialogs = this._activeDialogs.asReadonly();
  readonly queuedDialogs = this._queuedDialogs.asReadonly();
  readonly activeCount = computed(() => this._activeDialogs().length);
  readonly queuedCount = computed(() => this._queuedDialogs().length);
  readonly canOpenMore = computed(() => this.activeCount() < this.MAX_DIALOGS);

  // ═══════════════════════════════════════════════════════════════════
  // Public API - Confirm Dialogs
  // ═══════════════════════════════════════════════════════════════════

  async confirm(config: ConfirmDialogConfig | string): Promise<boolean> {
    const dialogConfig = this.normalizeConfirmConfig(config);
    const hash = this.createHash('confirm', dialogConfig.message, dialogConfig.title);

    if (!this.shouldOpenDialog(hash)) {
      return false;
    }

    const { ConfirmDialogComponent } = await import('../components/confirm-dialog.component');

    return this.openDialogWithQueue(
      hash,
      ConfirmDialogComponent,
      {
        ...DEFAULT_DIALOG_CONFIG,
        width: dialogConfig.width || DEFAULT_DIALOG_CONFIG.width,
        disableClose: dialogConfig.disableClose ?? DEFAULT_DIALOG_CONFIG.disableClose,
        data: dialogConfig
      },
      result => result?.confirmed ?? false
    );
  }

  async confirmDelete(message?: string): Promise<boolean> {
    return this.confirm({
      ...DIALOG_PRESETS.confirmDelete,
      message: message || DIALOG_PRESETS.confirmDelete.message
    });
  }

  async confirmDiscard(message?: string): Promise<boolean> {
    return this.confirm({
      ...DIALOG_PRESETS.confirmDiscard,
      message: message || DIALOG_PRESETS.confirmDiscard.message
    });
  }

  async confirmLeave(message?: string): Promise<boolean> {
    return this.confirm({
      ...DIALOG_PRESETS.confirmLeave,
      message: message || DIALOG_PRESETS.confirmLeave.message
    });
  }

  async confirmLogout(): Promise<boolean> {
    return this.confirm(DIALOG_PRESETS.confirmLogout);
  }

  // ═══════════════════════════════════════════════════════════════════
  // Public API - Alert Dialogs
  // ═══════════════════════════════════════════════════════════════════

  async alert(config: AlertDialogConfig | string): Promise<void> {
    const dialogConfig = this.normalizeAlertConfig(config);
    const hash = this.createHash('alert', dialogConfig.message, dialogConfig.title);

    if (!this.shouldOpenDialog(hash)) {
      return;
    }

    const { AlertDialogComponent } = await import('../components/alert-dialog.component');

    await this.openDialogWithQueue(
      hash,
      AlertDialogComponent,
      {
        ...DEFAULT_DIALOG_CONFIG,
        width: dialogConfig.width || DEFAULT_DIALOG_CONFIG.width,
        disableClose: dialogConfig.disableClose ?? DEFAULT_DIALOG_CONFIG.disableClose,
        data: dialogConfig
      }
    );
  }

  async alertSuccess(message: string): Promise<void> {
    return this.alert({ ...DIALOG_PRESETS.success, message });
  }

  async alertError(message: string): Promise<void> {
    return this.alert({ ...DIALOG_PRESETS.error, message });
  }

  async alertInfo(message: string): Promise<void> {
    return this.alert({ ...DIALOG_PRESETS.info, message });
  }

  async alertWarning(message: string): Promise<void> {
    return this.alert({ ...DIALOG_PRESETS.warning, message });
  }

  // ═══════════════════════════════════════════════════════════════════
  // Public API - Prompt Dialogs
  // ═══════════════════════════════════════════════════════════════════

  async prompt(config: PromptDialogConfig | string): Promise<string | null> {
    const dialogConfig = this.normalizePromptConfig(config);
    const hash = this.createHash('prompt', dialogConfig.message, dialogConfig.title);

    if (!this.shouldOpenDialog(hash)) {
      return null;
    }

    const { PromptDialogComponent } = await import('../components/prompt-dialog.component');

    return this.openDialogWithQueue(
      hash,
      PromptDialogComponent,
      {
        ...DEFAULT_DIALOG_CONFIG,
        width: dialogConfig.width || DEFAULT_DIALOG_CONFIG.width,
        disableClose: dialogConfig.disableClose ?? DEFAULT_DIALOG_CONFIG.disableClose,
        data: dialogConfig
      },
      result => (result?.submitted ? result.value ?? null : null)
    );
  }

  // ═══════════════════════════════════════════════════════════════════
  // Public API - Form Dialogs
  // ═══════════════════════════════════════════════════════════════════

  async openForm<T = any>(config: FormDialogConfig<T>): Promise<T | null> {
    const hash = this.createHash('form', config.component.name || 'form');

    if (!this.shouldOpenDialog(hash)) {
      return null;
    }

    return this.openDialogWithQueue(
      hash,
      config.component,
      {
        ...DEFAULT_DIALOG_CONFIG,
        width: config.width || '600px',
        disableClose: config.disableClose ?? true,
        data: config.data
      },
      result => (result?.submitted ? result.data ?? null : null)
    );
  }

  // ═══════════════════════════════════════════════════════════════════
  // Public API - Custom Dialogs
  // ═══════════════════════════════════════════════════════════════════

  openCustom<T, R = any>(config: CustomDialogConfig<T>): Observable<R | undefined> {
    const hash = this.createHash('custom', config.component.name || 'custom');

    if (!this.canOpenMore()) {
      return new Observable(observer => observer.next(undefined));
    }

    this.trackDialog(hash);

    const dialogRef = this.dialog.open<any, T, R>(config.component, {
      ...DEFAULT_DIALOG_CONFIG,
      ...config
    });

    const dialogId = this.generateDialogId();
    this._activeDialogs.update(dialogs => [...dialogs, dialogId]);

    dialogRef.afterClosed().subscribe(() => {
      this.removeDialog(dialogId);
      this.processQueue();
    });

    return dialogRef.afterClosed();
  }

  async openCustomAsync<T, R = any>(config: CustomDialogConfig<T>): Promise<R | undefined> {
    return firstValueFrom(this.openCustom<T, R>(config));
  }

  // ═══════════════════════════════════════════════════════════════════
  // Public API - Bottom Sheet
  // ═══════════════════════════════════════════════════════════════════

  async openBottomSheet<T, R = any>(config: CustomDialogConfig<T>): Promise<R | undefined> {
    return this.openCustomAsync<T, R>({
      ...config,
      position: { bottom: '0' },
      width: '100%',
      maxWidth: '100vw',
      panelClass: ['bottom-sheet-dialog', ...(config.panelClass || [])]
    });
  }

  // ═══════════════════════════════════════════════════════════════════
  // Public API - Side Panel
  // ═══════════════════════════════════════════════════════════════════

  async openSidePanel<T, R = any>(
    config: CustomDialogConfig<T>,
    side: 'left' | 'right' = 'right'
  ): Promise<R | undefined> {
    return this.openCustomAsync<T, R>({
      ...config,
      position: side === 'right' ? { right: '0' } : { left: '0' },
      width: config.width || '400px',
      height: '100%',
      maxWidth: '90vw',
      panelClass: [`side-panel-dialog-${side}`, ...(config.panelClass || [])]
    });
  }

  // ═══════════════════════════════════════════════════════════════════
  // Public API - Fullscreen
  // ═══════════════════════════════════════════════════════════════════

  async openFullscreen<T, R = any>(config: CustomDialogConfig<T>): Promise<R | undefined> {
    return this.openCustomAsync<T, R>({
      ...config,
      width: '100vw',
      height: '100vh',
      maxWidth: '100vw',
      maxHeight: '100vh',
      panelClass: ['fullscreen-dialog', ...(config.panelClass || [])]
    });
  }

  // ═══════════════════════════════════════════════════════════════════
  // Public API - Utility Methods
  // ═══════════════════════════════════════════════════════════════════

  closeAll(): void {
    this.dialog.closeAll();
    this._activeDialogs.set([]);
    this._queuedDialogs.set([]);
  }

  hasOpenDialogs(): boolean {
    return this.dialog.openDialogs.length > 0;
  }

  getDiagnostics() {
    return {
      activeCount: this.activeCount(),
      queuedCount: this.queuedCount(),
      canOpenMore: this.canOpenMore(),
      maxDialogs: this.MAX_DIALOGS,
      recentDialogsCount: this.recentDialogs.size,
      matDialogCount: this.dialog.openDialogs.length
    };
  }

  // ═══════════════════════════════════════════════════════════════════
  // Private Methods - Queue Management
  // ═══════════════════════════════════════════════════════════════════

  private async openDialogWithQueue<T, R>(
    hash: string,
    component: any,
    config: MatDialogConfig,
    resultMapper?: (result: any) => R
  ): Promise<R> {
    if (!this.canOpenMore()) {
      return this.queueDialog(hash, component, config, resultMapper);
    }

    return this.openDialogInternal(hash, component, config, resultMapper);
  }

  private async openDialogInternal<T, R>(
    hash: string,
    component: any,
    config: MatDialogConfig,
    resultMapper?: (result: any) => R
  ): Promise<R> {
    this.trackDialog(hash);

    const dialogRef = this.dialog.open(component, config);
    const dialogId = this.generateDialogId();
    this._activeDialogs.update(dialogs => [...dialogs, dialogId]);

    const result = await firstValueFrom(dialogRef.afterClosed());

    this.removeDialog(dialogId);
    this.processQueue();

    return resultMapper ? resultMapper(result) : result;
  }

  private queueDialog<T, R>(
    hash: string,
    component: any,
    config: MatDialogConfig,
    resultMapper?: (result: any) => R
  ): Promise<R> {
    return new Promise((resolve) => {
      const queuedDialog: QueuedDialog = {
        hash,
        component,
        config,
        resolve: async () => {
          const result = await this.openDialogInternal(hash, component, config, resultMapper);
          resolve(result);
        }
      };

      this._queuedDialogs.update(queue => [...queue, queuedDialog]);
    });
  }

  private processQueue(): void {
    if (!this.canOpenMore() || this.queuedCount() === 0) {
      return;
    }

    const queue = this._queuedDialogs();
    const nextDialog = queue[0];

    if (nextDialog) {
      this._queuedDialogs.update(q => q.slice(1));
      nextDialog.resolve();
    }
  }

  // ═══════════════════════════════════════════════════════════════════
  // Private Methods - Deduplication
  // ═══════════════════════════════════════════════════════════════════

  private shouldOpenDialog(hash: string): boolean {
    const lastShown = this.recentDialogs.get(hash);
    if (lastShown && Date.now() - lastShown < this.DEDUP_WINDOW_MS) {
      return false;
    }
    return true;
  }

  private trackDialog(hash: string): void {
    this.recentDialogs.set(hash, Date.now());
    this.cleanupRecentDialogs();
  }

  private cleanupRecentDialogs(): void {
    const now = Date.now();
    for (const [hash, timestamp] of this.recentDialogs.entries()) {
      if (now - timestamp > this.DEDUP_WINDOW_MS) {
        this.recentDialogs.delete(hash);
      }
    }
  }

  private createHash(type: string, message?: string, title?: string): string {
    return `${type}:${title || ''}:${message || ''}`;
  }

  // ═══════════════════════════════════════════════════════════════════
  // Private Methods - State Management
  // ═══════════════════════════════════════════════════════════════════

  private removeDialog(dialogId: string): void {
    this._activeDialogs.update(dialogs => dialogs.filter(id => id !== dialogId));
  }

  private generateDialogId(): string {
    return `dialog_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  // ═══════════════════════════════════════════════════════════════════
  // Private Methods - Config Normalization
  // ═══════════════════════════════════════════════════════════════════

  private normalizeConfirmConfig(config: ConfirmDialogConfig | string): ConfirmDialogConfig {
    if (typeof config === 'string') {
      return {
        message: config,
        confirmText: 'Confirm',
        cancelText: 'Cancel'
      };
    }
    return {
      confirmText: 'Confirm',
      cancelText: 'Cancel',
      ...config
    };
  }

  private normalizeAlertConfig(config: AlertDialogConfig | string): AlertDialogConfig {
    if (typeof config === 'string') {
      return {
        message: config,
        okText: 'OK'
      };
    }
    return {
      okText: 'OK',
      ...config
    };
  }

  private normalizePromptConfig(config: PromptDialogConfig | string): PromptDialogConfig {
    if (typeof config === 'string') {
      return {
        message: config,
        inputType: 'text',
        confirmText: 'Submit',
        cancelText: 'Cancel'
      };
    }
    return {
      inputType: 'text',
      confirmText: 'Submit',
      cancelText: 'Cancel',
      ...config
    };
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.closeAll();
  }
}

interface QueuedDialog {
  hash: string;
  component: any;
  config: MatDialogConfig;
  resolve: () => Promise<void>;
}
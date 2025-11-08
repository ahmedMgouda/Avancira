/**
 * Dialog Service
 * Central service for managing application dialogs
 * Provides strongly-typed, promise-based API for common dialog patterns
 */

import { inject,Injectable } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { firstValueFrom,Observable } from 'rxjs';

import {
  AlertDialogConfig,
  ConfirmDialogConfig,
  ConfirmDialogResult,
  CustomDialogConfig,
  DEFAULT_DIALOG_CONFIG,
  DIALOG_PRESETS,
  PromptDialogConfig,
  PromptDialogResult,
} from '../models/dialog.models';

@Injectable({
  providedIn: 'root',
})
export class DialogService {
  private readonly dialog = inject(MatDialog);

  /**
   * Display a confirmation dialog
   * @param config Configuration for the confirm dialog
   * @returns Promise resolving to true if confirmed, false if cancelled
   */
  async confirm(config: ConfirmDialogConfig | string): Promise<boolean> {
    const dialogConfig = this.normalizeConfirmConfig(config);
    
    // Lazy load the component
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

    const result = await firstValueFrom(dialogRef.afterClosed());
    return result?.confirmed ?? false;
  }

  /**
   * Display an alert dialog (information only)
   * @param config Configuration for the alert dialog
   * @returns Promise resolving when dialog is closed
   */
  async alert(config: AlertDialogConfig | string): Promise<void> {
    const dialogConfig = this.normalizeAlertConfig(config);
    
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

    await firstValueFrom(dialogRef.afterClosed());
  }

  /**
   * Display a prompt dialog to get user input
   * @param config Configuration for the prompt dialog
   * @returns Promise resolving to user input or null if cancelled
   */
  async prompt(config: PromptDialogConfig | string): Promise<string | null> {
    const dialogConfig = this.normalizePromptConfig(config);
    
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

    const result = await firstValueFrom(dialogRef.afterClosed());
    return result?.submitted ? (result.value ?? null) : null;
  }

  /**
   * Open a custom dialog with any component
   * @param config Configuration for the custom dialog
   * @returns Observable of dialog result
   */
  openCustom<T, R = any>(config: CustomDialogConfig<T>): Observable<R | undefined> {
    const dialogRef = this.dialog.open<any, T, R>(config.component, {
      ...DEFAULT_DIALOG_CONFIG,
      ...config,
    });

    return dialogRef.afterClosed();
  }

  /**
   * Open a custom dialog and return a promise
   * @param config Configuration for the custom dialog
   * @returns Promise of dialog result
   */
  async openCustomAsync<T, R = any>(config: CustomDialogConfig<T>): Promise<R | undefined> {
    return firstValueFrom(this.openCustom<T, R>(config));
  }

  // ===== Semantic Preset Methods =====

  /**
   * Show a delete confirmation dialog
   * @param message Optional custom message
   * @returns Promise resolving to true if confirmed
   */
  async confirmDelete(message?: string): Promise<boolean> {
    return this.confirm({
      ...DIALOG_PRESETS.confirmDelete,
      message: message || DIALOG_PRESETS.confirmDelete.message,
    });
  }

  /**
   * Show a discard changes confirmation dialog
   * @param message Optional custom message
   * @returns Promise resolving to true if confirmed
   */
  async confirmDiscard(message?: string): Promise<boolean> {
    return this.confirm({
      ...DIALOG_PRESETS.confirmDiscard,
      message: message || DIALOG_PRESETS.confirmDiscard.message,
    });
  }

  /**
   * Show a leave page confirmation dialog
   * @param message Optional custom message
   * @returns Promise resolving to true if confirmed
   */
  async confirmLeave(message?: string): Promise<boolean> {
    return this.confirm({
      ...DIALOG_PRESETS.confirmLeave,
      message: message || DIALOG_PRESETS.confirmLeave.message,
    });
  }

  /**
   * Show a logout confirmation dialog
   * @returns Promise resolving to true if confirmed
   */
  async confirmLogout(): Promise<boolean> {
    return this.confirm(DIALOG_PRESETS.confirmLogout);
  }

  /**
   * Show a success alert dialog
   * @param message Success message to display
   * @returns Promise resolving when dialog is closed
   */
  async alertSuccess(message: string): Promise<void> {
    return this.alert({
      ...DIALOG_PRESETS.success,
      message,
    });
  }

  /**
   * Show an error alert dialog
   * @param message Error message to display
   * @returns Promise resolving when dialog is closed
   */
  async alertError(message: string): Promise<void> {
    return this.alert({
      ...DIALOG_PRESETS.error,
      message,
    });
  }

  /**
   * Show an info alert dialog
   * @param message Info message to display
   * @returns Promise resolving when dialog is closed
   */
  async alertInfo(message: string): Promise<void> {
    return this.alert({
      ...DIALOG_PRESETS.info,
      message,
    });
  }

  /**
   * Show a warning alert dialog
   * @param message Warning message to display
   * @returns Promise resolving when dialog is closed
   */
  async alertWarning(message: string): Promise<void> {
    return this.alert({
      ...DIALOG_PRESETS.warning,
      message,
    });
  }

  /**
   * Close all open dialogs
   */
  closeAll(): void {
    this.dialog.closeAll();
  }

  /**
   * Check if any dialogs are currently open
   */
  hasOpenDialogs(): boolean {
    return this.dialog.openDialogs.length > 0;
  }

  // ===== Private Helper Methods =====

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
}
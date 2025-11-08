/**
 * Dialog Models & Types
 * Provides strong typing for dialog configurations and results
 */

import { ComponentType } from '@angular/cdk/portal';
import { MatDialogConfig } from '@angular/material/dialog';

/**
 * Dialog button configuration
 */
export interface DialogButton {
  text: string;
  value: any;
  color?: 'primary' | 'accent' | 'warn';
  autofocus?: boolean;
}

/**
 * Dialog types for semantic presets
 */
export type DialogType = 'info' | 'success' | 'warning' | 'danger';

/**
 * Icon configuration for dialogs
 */
export interface DialogIcon {
  name: string;
  color?: string;
}

/**
 * Base dialog configuration
 */
export interface BaseDialogConfig {
  title?: string;
  message: string;
  type?: DialogType;
  icon?: DialogIcon;
  width?: string;
  disableClose?: boolean;
}

/**
 * Confirm dialog configuration
 */
export interface ConfirmDialogConfig extends BaseDialogConfig {
  confirmText?: string;
  cancelText?: string;
  confirmColor?: 'primary' | 'accent' | 'warn';
}

/**
 * Alert dialog configuration
 */
export interface AlertDialogConfig extends BaseDialogConfig {
  okText?: string;
}

/**
 * Prompt dialog configuration
 */
export interface PromptDialogConfig extends BaseDialogConfig {
  placeholder?: string;
  defaultValue?: string;
  inputType?: 'text' | 'number' | 'email' | 'password' | 'textarea';
  required?: boolean;
  minLength?: number;
  maxLength?: number;
  pattern?: string;
  confirmText?: string;
  cancelText?: string;
}

/**
 * Custom dialog configuration
 */
export interface CustomDialogConfig<T = any> extends MatDialogConfig<T> {
  component: ComponentType<any>;
}

/**
 * Dialog result for confirm dialogs
 */
export interface ConfirmDialogResult {
  confirmed: boolean;
}

/**
 * Dialog result for prompt dialogs
 */
export interface PromptDialogResult {
  submitted: boolean;
  value?: string;
}

/**
 * Default dialog configurations
 */
export const DEFAULT_DIALOG_CONFIG = {
  width: '400px',
  maxWidth: '90vw',
  autoFocus: 'dialog',
  restoreFocus: true,
  disableClose: false,
} as const;

/**
 * Semantic dialog presets
 */
export const DIALOG_PRESETS = {
  confirmDelete: {
    title: 'Confirm Delete',
    message: 'Are you sure you want to delete this item? This action cannot be undone.',
    type: 'danger' as DialogType,
    icon: { name: 'delete', color: '#f44336' },
    confirmText: 'Delete',
    cancelText: 'Cancel',
    confirmColor: 'warn' as const,
  },
  confirmDiscard: {
    title: 'Discard Changes',
    message: 'You have unsaved changes. Are you sure you want to discard them?',
    type: 'warning' as DialogType,
    icon: { name: 'warning', color: '#ff9800' },
    confirmText: 'Discard',
    cancelText: 'Keep Editing',
  },
  confirmLeave: {
    title: 'Leave Page',
    message: 'Are you sure you want to leave this page? Any unsaved changes will be lost.',
    type: 'warning' as DialogType,
    icon: { name: 'exit_to_app', color: '#ff9800' },
    confirmText: 'Leave',
    cancelText: 'Stay',
  },
  confirmLogout: {
    title: 'Confirm Logout',
    message: 'Are you sure you want to log out?',
    type: 'info' as DialogType,
    icon: { name: 'logout', color: '#2196f3' },
    confirmText: 'Logout',
    cancelText: 'Cancel',
  },
  success: {
    title: 'Success',
    type: 'success' as DialogType,
    icon: { name: 'check_circle', color: '#4caf50' },
    okText: 'OK',
  },
  error: {
    title: 'Error',
    type: 'danger' as DialogType,
    icon: { name: 'error', color: '#f44336' },
    okText: 'OK',
  },
  info: {
    title: 'Information',
    type: 'info' as DialogType,
    icon: { name: 'info', color: '#2196f3' },
    okText: 'OK',
  },
  warning: {
    title: 'Warning',
    type: 'warning' as DialogType,
    icon: { name: 'warning', color: '#ff9800' },
    okText: 'OK',
  },
} as const;
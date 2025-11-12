export type ToastType = 'success' | 'error' | 'warning' | 'info';

export type ToastPosition =
  | 'top-left'
  | 'top-center'
  | 'top-right'
  | 'bottom-left'
  | 'bottom-center'
  | 'bottom-right';

export interface ToastAction {
  label: string;
  action: () => void;
}

export interface Toast {
  id: string;
  type: ToastType;
  title?: string;
  message: string;
  duration?: number; // ms, 0 = permanent
  dismissible?: boolean;
  action?: ToastAction;
  icon?: string;
  timestamp: number;
}

export interface ToastConfig {
  maxVisible?: number;
  defaultDuration?: number;
  position?: ToastPosition;
}

/**
 * Internal model for tracking recent toasts
 */
export interface ToastRecord {
  hash: string;
  lastShown: Date;
  suppressedCount: number;
  type: ToastType;
  message: string;
  title?: string;
}

/**
 * Request model for showing toasts
 */
export interface ToastRequest {
  type: ToastType;
  message: string;
  title?: string;
  duration?: number;
  action?: ToastAction;
  dismissible?: boolean;
}
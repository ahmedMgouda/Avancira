// Models
export type { 
  Toast, 
  ToastAction, 
  ToastConfig, 
  ToastPosition, 
  ToastRecord,
  ToastRequest,
  ToastType 
} from './models/toast.model';

// Services
export { ToastService } from './services/toast.service';
export { ToastManager } from './services/toast-manager.service';

// Components
export { ToastContainerComponent } from './components/toast-container.component';

/**
 * ═════════════════════════════════════════════════════════════════════
 * Usage Examples
 * ═════════════════════════════════════════════════════════════════════
 * 
 * 1. Basic Usage (via ToastManager):
 * ```typescript
 * import { ToastManager } from '@core/toast';
 * 
 * export class MyComponent {
 *   private toast = inject(ToastManager);
 *   
 *   showSuccess() {
 *     this.toast.success('Operation completed!');
 *   }
 *   
 *   showError() {
 *     this.toast.error('Something went wrong', 'Error');
 *   }
 * }
 * ```
 * 
 * 2. With Action Button:
 * ```typescript
 * this.toast.showWithAction(
 *   'warning',
 *   'Connection lost',
 *   { label: 'Retry', action: () => this.retry() },
 *   'Network Error'
 * );
 * ```
 * 
 * 3. Deduplication (automatic):
 * ```typescript
 * // These will be deduplicated if shown within time window
 * this.toast.error('Network error');  // Shown
 * this.toast.error('Network error');  // Suppressed
 * this.toast.error('Network error');  // Suppressed
 * // After 3+ suppressions: "Network error (3 similar suppressed)"
 * ```
 * 
 * 4. Diagnostics:
 * ```typescript
 * const diagnostics = this.toast.getDiagnostics();
 * console.log(diagnostics.totalSuppressed);
 * console.log(diagnostics.suppressionsByType);
 * ```
 */
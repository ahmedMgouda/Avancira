import { DestroyRef,effect, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged,Subject } from 'rxjs';

import { ToastManager } from '../../toast/services/toast-manager.service';
import { NetworkService } from './network.service';
import { NetworkStatus } from './network.service';

/**
 * ═══════════════════════════════════════════════════════════════════════
 * NETWORK NOTIFICATION SERVICE
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * Single Responsibility: User-facing notifications for network state changes
 * 
 * RESPONSIBILITIES:
 * ----------------
 * ✅ Subscribe to NetworkService state changes
 * ✅ Show/dismiss notifications appropriately
 * ✅ Debounce and throttle notification spam
 * ✅ Provide manual retry functionality
 * ✅ Coordinate notification lifecycle
 * 
 * DOES NOT:
 * ---------
 * ❌ Perform health checks (NetworkService's job)
 * ❌ Track errors (NetworkService's job)
 * ❌ Classify errors (ErrorClassifier's job)
 * ❌ Handle HTTP errors (Interceptor's job)
 * 
 * NOTIFICATION STRATEGY:
 * ---------------------
 * 1. Offline Detection (Instant)
 *    - Browser goes offline → Show "No internet" (persistent)
 *    - Browser comes online → Dismiss offline, show "Restored" (5s)
 * 
 * 2. Server Unreachable (Debounced)
 *    - Health checks fail → Wait 500ms to group errors
 *    - Show "Server unreachable" with retry button (persistent)
 *    - Health restored → Dismiss server toast, show "Reconnected" (5s)
 * 
 * 3. Debouncing
 *    - Groups rapid state changes within 500ms window
 *    - Prevents notification spam during burst failures
 *    - Only notifies on stable state changes
 */

@Injectable({ providedIn: 'root' })
export class NetworkNotificationService {
  private readonly networkService = inject(NetworkService);
  private readonly toast = inject(ToastManager);
  private readonly destroyRef = inject(DestroyRef);

  // State tracking
  private readonly _isRetrying = signal(false);
  private readonly _lastNotifiedState = signal<'online' | 'offline' | 'server-issue' | null>(null);

  // Notification IDs for lifecycle management
  private offlineNotificationId: string | null = null;
  private serverIssueNotificationId: string | null = null;

  // Debounce subject for state changes
  private readonly stateChange$ = new Subject<NetworkStatus>();

  // Public signals
  readonly isRetrying = this._isRetrying.asReadonly();

  constructor() {
    this.initializeStateMonitoring();
    this.initializeDebouncedNotifications();
  }

  // ═══════════════════════════════════════════════════════════════════════
  // Public API
  // ═══════════════════════════════════════════════════════════════════════

  /**
   * Manually retry connection
   * Triggers immediate health check and shows loading state
   */
  async retryConnection(): Promise<void> {
    if (this._isRetrying()) return;

    this._isRetrying.set(true);

    try {
      // Trigger immediate health check
      await this.networkService.performImmediateHealthCheck();
      
      // Wait a moment for state to stabilize
      await this.delay(500);
    } finally {
      this._isRetrying.set(false);
    }
  }

  /**
   * Dismiss all network notifications
   */
  dismissAllNotifications(): void {
    if (this.offlineNotificationId) {
      this.toast.dismiss(this.offlineNotificationId);
      this.offlineNotificationId = null;
    }

    if (this.serverIssueNotificationId) {
      this.toast.dismiss(this.serverIssueNotificationId);
      this.serverIssueNotificationId = null;
    }
  }

  // ═══════════════════════════════════════════════════════════════════════
  // Private Methods - State Monitoring
  // ═══════════════════════════════════════════════════════════════════════

  /**
   * Initialize real-time state monitoring
   */
  private initializeStateMonitoring(): void {
    // Monitor network state changes
    effect(() => {
      const state = this.networkService.networkState();
      this.stateChange$.next(state);
    });
  }

  /**
   * Initialize debounced notification handling
   * Groups rapid state changes to prevent notification spam
   */
  private initializeDebouncedNotifications(): void {
    this.stateChange$
      .pipe(
        debounceTime(500), // Wait 500ms for state to stabilize
        distinctUntilChanged((prev, curr) => 
          prev.online === curr.online && 
          prev.healthy === curr.healthy
        ),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(state => {
        this.handleStateChange(state);
      });
  }

  /**
   * Handle network state changes with proper notification lifecycle
   */
  private handleStateChange(state: NetworkStatus): void {
    const currentState = this.determineNotificationState(state);
    const previousState = this._lastNotifiedState();

    // Skip if no state change
    if (currentState === previousState) {
      return;
    }

    // Handle state transition
    switch (currentState) {
      case 'offline':
        this.handleOfflineState();
        break;

      case 'server-issue':
        this.handleServerIssueState(state);
        break;

      case 'online':
        this.handleOnlineState(previousState);
        break;
    }

    this._lastNotifiedState.set(currentState);
  }

  /**
   * Determine notification state from network status
   */
  private determineNotificationState(
    state: NetworkStatus
  ): 'online' | 'offline' | 'server-issue' {
    if (!state.online) {
      return 'offline';
    }

    if (!state.healthy) {
      return 'server-issue';
    }

    return 'online';
  }

  // ═══════════════════════════════════════════════════════════════════════
  // Private Methods - Notification Handlers
  // ═══════════════════════════════════════════════════════════════════════

  /**
   * Handle offline state
   * Shows persistent notification with no retry (browser issue)
   */
  private handleOfflineState(): void {
    // Dismiss any server issue notifications
    if (this.serverIssueNotificationId) {
      this.toast.dismiss(this.serverIssueNotificationId);
      this.serverIssueNotificationId = null;
    }

    // Show offline notification if not already showing
    if (!this.offlineNotificationId) {
      this.offlineNotificationId = this.toast.error(
        'You appear to be offline. Please check your internet connection.',
        'No Internet Connection',
        0 // Persistent
      );
    }
  }

  /**
   * Handle server issue state
   * Shows persistent notification with retry button
   */
  private handleServerIssueState(state: NetworkStatus): void {
    // Dismiss offline notification (we're online, just server unreachable)
    if (this.offlineNotificationId) {
      this.toast.dismiss(this.offlineNotificationId);
      this.offlineNotificationId = null;
    }

    // Show server issue notification if not already showing
    if (!this.serverIssueNotificationId) {
      this.serverIssueNotificationId = this.toast.showWithAction(
        'warning',
        `Unable to reach the server after ${state.consecutiveErrors} attempts. ` +
        `This could be due to server maintenance or network issues.`,
        {
          label: 'Retry Now',
          action: () => this.retryConnection()
        },
        'Server Unreachable',
        0 // Persistent
      );
    }
  }

  /**
   * Handle online/healthy state
   * Shows restoration notification based on previous state
   */
  private handleOnlineState(
    previousState: 'online' | 'offline' | 'server-issue' | null
  ): void {
    // Dismiss all error notifications
    this.dismissAllNotifications();

    // Show appropriate restoration message
    if (previousState === 'offline') {
      this.toast.success(
        'Your internet connection has been restored.',
        'Connection Restored',
        5000 // Auto-dismiss after 5s
      );
    } else if (previousState === 'server-issue') {
      this.toast.success(
        'Connection to the server has been restored.',
        'Server Reconnected',
        5000 // Auto-dismiss after 5s
      );
    }
    // Don't show notification if previous state was also online
  }

  // ═══════════════════════════════════════════════════════════════════════
  // Utilities
  // ═══════════════════════════════════════════════════════════════════════

  private delay(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  // ═══════════════════════════════════════════════════════════════════════
  // Diagnostics
  // ═══════════════════════════════════════════════════════════════════════

  getDiagnostics() {
    return {
      isRetrying: this._isRetrying(),
      lastNotifiedState: this._lastNotifiedState(),
      activeNotifications: {
        offline: !!this.offlineNotificationId,
        serverIssue: !!this.serverIssueNotificationId,
        offlineId: this.offlineNotificationId,
        serverIssueId: this.serverIssueNotificationId
      },
      networkState: this.networkService.networkState(),
      networkDiagnostics: this.networkService.getDiagnostics()
    };
  }
}
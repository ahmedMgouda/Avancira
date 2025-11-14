import { DestroyRef, effect, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';

import { ToastManager } from '../../toast/services/toast-manager.service';
import { NetworkService } from './network.service';
import { NetworkStatus } from './network.service';

/**
 * ═══════════════════════════════════════════════════════════════════════
 * NETWORK NOTIFICATION SERVICE - FIXED
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * FIXES:
 * ✅ Resets lastNotifiedState when user dismisses toast manually
 * ✅ Tracks toast lifecycle with onDismiss callback
 * ✅ Allows notifications to re-show after manual dismissal
 */

@Injectable({ providedIn: 'root' })
export class NetworkNotificationService {
  private readonly networkService = inject(NetworkService);
  private readonly toast = inject(ToastManager);
  private readonly destroyRef = inject(DestroyRef);

  private readonly _isRetrying = signal(false);
  private readonly _lastNotifiedState = signal<'online' | 'offline' | 'server-issue' | null>(null);

  private offlineNotificationId: string | null = null;
  private serverIssueNotificationId: string | null = null;

  private readonly stateChange$ = new Subject<NetworkStatus>();

  readonly isRetrying = this._isRetrying.asReadonly();

  constructor() {
    this.initializeStateMonitoring();
    this.initializeDebouncedNotifications();
  }

  // ═══════════════════════════════════════════════════════════════════════
  // Public API
  // ═══════════════════════════════════════════════════════════════════════

  async retryConnection(): Promise<void> {
    if (this._isRetrying()) return;

    this._isRetrying.set(true);

    try {
      await this.networkService.performImmediateHealthCheck();
      await this.delay(500);
    } finally {
      this._isRetrying.set(false);
    }
  }

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
  // Private Methods
  // ═══════════════════════════════════════════════════════════════════════

  private initializeStateMonitoring(): void {
    effect(() => {
      const state = this.networkService.networkState();
      this.stateChange$.next(state);
    });
  }

  private initializeDebouncedNotifications(): void {
    this.stateChange$
      .pipe(
        debounceTime(500),
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

  private handleStateChange(state: NetworkStatus): void {
    const currentState = this.determineNotificationState(state);
    const previousState = this._lastNotifiedState();

    if (currentState === previousState) {
      return;
    }

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
  // Notification Handlers - FIXED
  // ═══════════════════════════════════════════════════════════════════════

  private handleOfflineState(): void {
    if (this.serverIssueNotificationId) {
      this.toast.dismiss(this.serverIssueNotificationId);
      this.serverIssueNotificationId = null;
    }

    if (!this.offlineNotificationId) {
      this.offlineNotificationId = this.toast.error(
        'You appear to be offline. Please check your internet connection.',
        'No Internet Connection',
        0, // Persistent
        () => {
          // FIX: Reset state when user manually dismisses
          this._lastNotifiedState.set(null);
          this.offlineNotificationId = null;
        }
      );
    }
  }

  private handleServerIssueState(state: NetworkStatus): void {
    if (this.offlineNotificationId) {
      this.toast.dismiss(this.offlineNotificationId);
      this.offlineNotificationId = null;
    }

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
        0, // Persistent
        () => {
          // FIX: Reset state when user manually dismisses
          this._lastNotifiedState.set(null);
          this.serverIssueNotificationId = null;
        }
      );
    }
  }

  private handleOnlineState(
    previousState: 'online' | 'offline' | 'server-issue' | null
  ): void {
    this.dismissAllNotifications();

    if (previousState === 'offline') {
      this.toast.success(
        'Your internet connection has been restored.',
        'Connection Restored',
        5000
      );
    } else if (previousState === 'server-issue') {
      this.toast.success(
        'Connection to the server has been restored.',
        'Server Reconnected',
        5000
      );
    }
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

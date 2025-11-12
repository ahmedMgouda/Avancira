import { computed, Injectable, signal } from '@angular/core';

import { getLoadingConfig } from '../../config/loading.config';

interface GlobalLoadingState {
  active: boolean;
  message?: string;
}

interface RouteLoadingState {
  active: boolean;
  from?: string;
  to?: string;
}

@Injectable({ providedIn: 'root' })
export class LoadingService {
  private readonly config = getLoadingConfig();

  // State
  private readonly _global = signal<GlobalLoadingState>({ active: false });
  private readonly _route = signal<RouteLoadingState>({ active: false });
  private readonly activeRequests = new Set<string>();
  private readonly _activeCount = signal(0);

  // Timers
  private readonly debounceTimers = new Map<string, ReturnType<typeof setTimeout>>();
  private readonly timeoutTimers = new Map<string, ReturnType<typeof setTimeout>>();

  // Public signals
  readonly isGlobalLoading = computed(() => this._global().active);
  readonly globalMessage = computed(() => this._global().message);
  readonly isRouteLoading = computed(() => this._route().active);
  readonly isHttpLoading = computed(() => this._activeCount() > 0);
  readonly activeCount = this._activeCount.asReadonly();
  readonly isAnyLoading = computed(() => 
    this.isGlobalLoading() || this.isRouteLoading() || this.isHttpLoading()
  );

  // Global loading
  showGlobal(message?: string): void {
    this._global.set({ active: true, message });
  }

  hideGlobal(): void {
    this._global.set({ active: false });
  }

  updateGlobalMessage(message: string): void {
    if (this._global().active) {
      this._global.set({ active: true, message });
    }
  }

  // Route loading
  startRouteLoading(from?: string, to?: string): void {
    this._route.set({ active: true, from, to });
  }

  completeRouteLoading(): void {
    this._route.set({ active: false });
  }

  // HTTP request tracking
  startRequest(id: string): void {
    if (this.activeRequests.size >= this.config.maxRequests) {
      console.warn(`[Loading] Max requests (${this.config.maxRequests}) reached`);
      return;
    }

    this.clearTimer(this.debounceTimers, id);

    const timer = setTimeout(() => {
      this.activeRequests.add(id);
      this._activeCount.set(this.activeRequests.size);
      this.setTimeoutProtection(id);
      this.debounceTimers.delete(id);
    }, this.config.debounceDelay);

    this.debounceTimers.set(id, timer);
  }

  completeRequest(id: string): void {
    this.clearTimer(this.debounceTimers, id);
    this.clearTimer(this.timeoutTimers, id);

    if (this.activeRequests.delete(id)) {
      this._activeCount.set(this.activeRequests.size);
    }
  }

  // Utilities
  clearAll(): void {
    this.clearAllTimers();
    this._global.set({ active: false });
    this._route.set({ active: false });
    this.activeRequests.clear();
    this._activeCount.set(0);
  }

  getDiagnostics() {
    return {
      globalLoading: this.isGlobalLoading(),
      routeLoading: this.isRouteLoading(),
      httpLoading: this.isHttpLoading(),
      activeRequests: this._activeCount(),
      config: this.config,
      timers: {
        debounce: this.debounceTimers.size,
        timeout: this.timeoutTimers.size
      }
    };
  }

  // Private helpers
  private setTimeoutProtection(id: string): void {
    const timer = setTimeout(() => {
      console.warn(`[Loading] Request timed out after ${this.config.requestTimeout}ms`);
      this.completeRequest(id);
    }, this.config.requestTimeout);

    this.timeoutTimers.set(id, timer);
  }

  private clearTimer(map: Map<string, ReturnType<typeof setTimeout>>, id: string): void {
    const timer = map.get(id);
    if (timer) {
      clearTimeout(timer);
      map.delete(id);
    }
  }

  private clearAllTimers(): void {
    this.debounceTimers.forEach(t => clearTimeout(t));
    this.debounceTimers.clear();
    this.timeoutTimers.forEach(t => clearTimeout(t));
    this.timeoutTimers.clear();
  }
}
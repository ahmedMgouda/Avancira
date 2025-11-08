/**
 * Loading Service - Phase 3 Refactored
 * ═══════════════════════════════════════════════════════════════════════
 * Phase 3 Improvements:
 *   ✅ Uses CleanupManager for all resource management
 *   ✅ Reduced microtask batching (only when >5 updates pending)
 *   ✅ Better error handling in update queue
 *   ✅ Simplified state management
 * 
 * Changes:
 *   - Extends CleanableService for automatic cleanup
 *   - Conditional microtask batching for performance
 *   - All timers tracked via CleanupManager
 */

import { computed, inject, Injectable, InjectionToken, signal } from '@angular/core';

import { CleanableService } from '../../utils/cleanup-manager';

export const LOADING_CONFIG = new InjectionToken<LoadingConfig>('LOADING_CONFIG', {
  providedIn: 'root',
  factory: () => ({
    debounceDelay: 200,
    requestTimeout: 30000,
    maxRequests: 100,
    maxOperations: 50,
    errorRetentionTime: 5000,
    microtaskBatchThreshold: 5 // ✅ NEW: Only batch when >5 pending updates
  })
});

export interface LoadingConfig {
  debounceDelay: number;
  requestTimeout: number;
  maxRequests: number;
  maxOperations: number;
  errorRetentionTime: number;
  microtaskBatchThreshold?: number;
}

interface GlobalLoadingState {
  active: boolean;
  message?: string;
}

interface RouteLoadingState {
  active: boolean;
  from?: string;
  to?: string;
}

export interface RequestInfo {
  id: string;
  startTime: number;
  method?: string;
  url?: string;
  group?: string;
}

export interface OperationInfo {
  key: string;
  message: string;
  progress: number | null;
  startTime: number;
}

export interface RequestMetadata {
  method?: string;
  url?: string;
  group?: string;
}

export interface LoadingDiagnostics {
  globalLoading: boolean;
  routeLoading: boolean;
  activeRequests: number;
  activeOperations: number;
  anyLoading: boolean;
  hasErrors: boolean;
  requests: Array<RequestInfo & { duration: number }>;
  operations: Array<OperationInfo & { duration: number }>;
  pendingDebounceTimers: number;
  pendingTimeoutTimers: number;
}

@Injectable({ providedIn: 'root' })
export class LoadingService extends CleanableService {
  private readonly config = inject(LOADING_CONFIG);
  
  // State signals
  private readonly _global = signal<GlobalLoadingState>({ active: false });
  private readonly _route = signal<RouteLoadingState>({ active: false });
  private readonly _requests = signal<ReadonlyMap<string, RequestInfo>>(new Map());
  private readonly _operations = signal<ReadonlyMap<string, OperationInfo>>(new Map());
  private readonly _errors = signal<ReadonlyMap<string, Error>>(new Map());
  
  // ✅ OPTIMIZED: Conditional microtask batching
  private pendingUpdates: (() => void)[] = [];
  private updateScheduled = false;
  private readonly batchThreshold: number;
  
  // Timer tracking (now via CleanupManager)
  private debounceTimers = new Map<string, ReturnType<typeof setTimeout>>();
  
  // Public computed signals
  readonly isGlobalLoading = computed(() => this._global().active);
  readonly globalMessage = computed(() => this._global().message);
  readonly isRouteLoading = computed(() => this._route().active);
  readonly isHttpLoading = computed(() => this._requests().size > 0);
  readonly activeRequestCount = computed(() => this._requests().size);
  readonly isOperationLoading = computed(() => this._operations().size > 0);
  readonly activeOperationCount = computed(() => this._operations().size);
  readonly hasRequestErrors = computed(() => this._errors().size > 0);
  readonly isAnyLoading = computed(() =>
    this.isGlobalLoading() || 
    this.isRouteLoading() || 
    this.isHttpLoading() || 
    this.isOperationLoading()
  );
  
  constructor() {
    super(); // Initialize CleanupManager
    this.batchThreshold = this.config.microtaskBatchThreshold ?? 5;
  }
  
  // ═════════════════════════════════════════════════════════════════════
  // ✅ OPTIMIZED: Microtask Batching (Conditional)
  // ═════════════════════════════════════════════════════════════════════
  
  /**
   * Schedule update with conditional batching
   * Only uses microtask batching when >5 updates are pending
   */
  private scheduleUpdate(updateFn: () => void): void {
    this.pendingUpdates.push(updateFn);
    
    // ✅ NEW: Conditional batching
    if (this.pendingUpdates.length >= this.batchThreshold) {
      // Many updates pending - use batching
      if (!this.updateScheduled) {
        this.updateScheduled = true;
        queueMicrotask(() => this.flushUpdates());
      }
    } else {
      // Few updates - execute immediately
      this.flushUpdates();
    }
  }
  
  /**
   * Flush all pending updates
   */
  private flushUpdates(): void {
    const updates = this.pendingUpdates.slice();
    this.pendingUpdates = [];
    this.updateScheduled = false;
    
    // ✅ Better error handling
    for (const updateFn of updates) {
      try {
        updateFn();
      } catch (error) {
        console.error('[LoadingService] Update failed:', error);
      }
    }
  }
  
  // ═════════════════════════════════════════════════════════════════════
  // Global Loading
  // ═════════════════════════════════════════════════════════════════════
  
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
  
  // ═════════════════════════════════════════════════════════════════════
  // Route Loading
  // ═════════════════════════════════════════════════════════════════════
  
  startRouteLoading(from?: string, to?: string): void {
    this._route.set({ active: true, from, to });
  }
  
  completeRouteLoading(): void {
    this._route.set({ active: false });
  }
  
  // ═════════════════════════════════════════════════════════════════════
  // HTTP Request Tracking
  // ═════════════════════════════════════════════════════════════════════
  
  startRequest(id: string, metadata?: RequestMetadata): void {
    this.clearDebounceTimer(id);
    
    // ✅ Use CleanupManager for timeout
    const debounceTimer = this.cleanup.setTimeout(() => {
      this.scheduleUpdate(() => {
        if (this._requests().size >= this.config.maxRequests) {
          this.clearOldestRequest();
        }
        
        const info: RequestInfo = {
          id,
          startTime: Date.now(),
          method: metadata?.method,
          url: metadata?.url,
          group: metadata?.group
        };
        
        this._requests.update(map => {
          const newMap = new Map(map);
          newMap.set(id, info);
          return newMap;
        });
      });
      
      this.setTimeoutProtection(id);
      this.debounceTimers.delete(id);
    }, this.config.debounceDelay);
    
    this.debounceTimers.set(id, debounceTimer);
  }
  
  completeRequest(id: string, error?: Error): void {
    this.clearDebounceTimer(id);
    
    const wasTracked = this._requests().has(id);
    const hasPendingTimer = this.debounceTimers.has(id);
    
    if (!wasTracked && !hasPendingTimer) {
      return;
    }
    
    this.scheduleUpdate(() => {
      if (error) {
        this._errors.update(map => {
          const newMap = new Map(map);
          newMap.set(id, error);
          return newMap;
        });
        
        // ✅ Use CleanupManager for error retention
        this.cleanup.setTimeout(() => {
          this._errors.update(map => {
            const newMap = new Map(map);
            newMap.delete(id);
            return newMap;
          });
        }, this.config.errorRetentionTime);
      }
      
      this._requests.update(map => {
        const newMap = new Map(map);
        newMap.delete(id);
        return newMap;
      });
    });
  }
  
  getRequest(id: string): RequestInfo | undefined {
    return this._requests().get(id);
  }
  
  getRequestsByGroup(group: string): RequestInfo[] {
    return Array.from(this._requests().values())
      .filter(req => req.group === group);
  }
  
  // ═════════════════════════════════════════════════════════════════════
  // Operation Tracking
  // ═════════════════════════════════════════════════════════════════════
  
  startOperation(key: string, message?: string, progress?: number): string {
    this.scheduleUpdate(() => {
      if (this._operations().size >= this.config.maxOperations) {
        this.clearOldestOperation();
      }
      
      const info: OperationInfo = {
        key,
        message: message ?? '',
        progress: progress ?? null,
        startTime: Date.now()
      };
      
      this._operations.update(map => {
        const newMap = new Map(map);
        newMap.set(key, info);
        return newMap;
      });
    });
    
    return key;
  }
  
  updateOperation(key: string, message: string, progress?: number): void {
    const operation = this._operations().get(key);
    if (!operation) return;
    
    this._operations.update(map => {
      const newMap = new Map(map);
      newMap.set(key, {
        ...operation,
        message,
        progress: progress ?? operation.progress
      });
      return newMap;
    });
  }
  
  completeOperation(key: string): void {
    this._operations.update(map => {
      const newMap = new Map(map);
      newMap.delete(key);
      return newMap;
    });
  }
  
  getOperation(key: string): OperationInfo | undefined {
    return this._operations().get(key);
  }
  
  isOperationActive(key: string): boolean {
    return this._operations().has(key);
  }
  
  // ═════════════════════════════════════════════════════════════════════
  // Utility Methods
  // ═════════════════════════════════════════════════════════════════════
  
  clearAll(): void {
    // Cancel all pending updates
    this.pendingUpdates = [];
    this.updateScheduled = false;
    
    // Clear all timers via CleanupManager
    this.cleanup.cleanup();
    this.debounceTimers.clear();
    
    // Reset state
    this._global.set({ active: false });
    this._route.set({ active: false });
    this._requests.set(new Map());
    this._operations.set(new Map());
    this._errors.set(new Map());
  }
  
  getDiagnostics(): LoadingDiagnostics {
    const now = Date.now();
    
    return {
      globalLoading: this.isGlobalLoading(),
      routeLoading: this.isRouteLoading(),
      activeRequests: this.activeRequestCount(),
      activeOperations: this.activeOperationCount(),
      anyLoading: this.isAnyLoading(),
      hasErrors: this.hasRequestErrors(),
      requests: Array.from(this._requests().values()).map(r => ({
        ...r,
        duration: now - r.startTime
      })),
      operations: Array.from(this._operations().values()).map(op => ({
        ...op,
        duration: now - op.startTime
      })),
      pendingDebounceTimers: this.debounceTimers.size,
      pendingTimeoutTimers: 0 // Tracked by CleanupManager now
    };
  }
  
  // ═════════════════════════════════════════════════════════════════════
  // Private Helper Methods
  // ═════════════════════════════════════════════════════════════════════
  
  private setTimeoutProtection(id: string): void {
    // ✅ Use CleanupManager
    this.cleanup.setTimeout(() => {
      console.warn(`[LoadingService] Request ${id} timed out after ${this.config.requestTimeout}ms`);
      this.completeRequest(id);
    }, this.config.requestTimeout);
  }
  
  private clearDebounceTimer(id: string): void {
    const timer = this.debounceTimers.get(id);
    if (timer) {
      this.cleanup.clearTimeout(timer);
      this.debounceTimers.delete(id);
    }
  }
  
  private clearOldestRequest(): void {
    const requests = Array.from(this._requests().entries());
    if (requests.length === 0) return;
    
    const oldest = requests.reduce((prev, curr) => 
      curr[1].startTime < prev[1].startTime ? curr : prev
    );
    
    console.warn(`[LoadingService] Max requests reached, clearing oldest: ${oldest[0]}`);
    this.completeRequest(oldest[0]);
  }
  
  private clearOldestOperation(): void {
    const operations = Array.from(this._operations().entries());
    if (operations.length === 0) return;
    
    const oldest = operations.reduce((prev, curr) => 
      curr[1].startTime < prev[1].startTime ? curr : prev
    );
    
    console.warn(`[LoadingService] Max operations reached, clearing oldest: ${oldest[0]}`);
    this.completeOperation(oldest[0]);
  }
  
  // ✅ Cleanup hook from CleanableService
  protected override onCleanup(): void {
    this.pendingUpdates = [];
    this.updateScheduled = false;
    this.debounceTimers.clear();
  }
}
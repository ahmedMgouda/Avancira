/**
 * Trace Manager Service
 * Manages trace lifecycle (creation, activation, ending)
 * 
 * RESPONSIBILITIES:
 * - Create new traces on navigation
 * - Track active trace
 * - End traces on navigation change
 * - Provide trace context to spans
 */

import { Injectable, signal, computed } from '@angular/core';
import { Trace, TraceStatus, TraceOptions } from '../models/trace.model';

@Injectable({ providedIn: 'root' })
export class TraceManagerService {
  // ─────────────────────────────────────────────────────────
  // State
  // ─────────────────────────────────────────────────────────
  
  private readonly _currentTrace = signal<Trace | null>(null);
  private readonly _traces = signal<Trace[]>([]);
  
  /** Current active trace (reactive) */
  readonly currentTrace = this._currentTrace.asReadonly();
  
  /** All traces in current session (for debugging) */
  readonly traces = this._traces.asReadonly();
  
  /** Whether a trace is currently active */
  readonly hasActiveTrace = computed(() => this._currentTrace() !== null);
  
  // ─────────────────────────────────────────────────────────
  // Trace Creation
  // ─────────────────────────────────────────────────────────
  
  /**
   * Create a new trace
   * Called on navigation start
   * 
   * @param options Trace creation options
   * @returns Created trace
   */
  createTrace(options: TraceOptions): Trace {
    // End current trace if exists
    const currentTrace = this._currentTrace();
    if (currentTrace) {
      this.endTrace(currentTrace.traceId);
    }
    
    // Create new trace
    const trace: Trace = {
      traceId: this.generateTraceId(),
      startTime: new Date(),
      endTime: null,
      status: TraceStatus.ACTIVE,
      route: options.route,
      previousRoute: options.previousRoute,
      sessionId: options.sessionId,
      userId: options.userId,
      spanCount: 0,
      trigger: options.trigger || 'user'
    };
    
    // Set as current trace
    this._currentTrace.set(trace);
    
    // Add to traces history
    this._traces.update(traces => [...traces, trace]);
    
    console.log(
      `%c🔷 Trace Created: ${trace.traceId}`,
      'color: #3b82f6; font-weight: bold',
      {
        route: trace.route,
        previousRoute: trace.previousRoute,
        trigger: trace.trigger
      }
    );
    
    return trace;
  }
  
  /**
   * End a trace
   * Called on navigation or manually
   * 
   * @param traceId Trace ID to end
   */
  endTrace(traceId: string): void {
    const currentTrace = this._currentTrace();
    
    if (!currentTrace || currentTrace.traceId !== traceId) {
      console.warn(`Cannot end trace ${traceId}: not the current trace`);
      return;
    }
    
    // Update trace status
    const updatedTrace: Trace = {
      ...currentTrace,
      endTime: new Date(),
      status: TraceStatus.ENDED
    };
    
    // Update in history
    this._traces.update(traces =>
      traces.map(t => t.traceId === traceId ? updatedTrace : t)
    );
    
    // Clear current trace
    this._currentTrace.set(null);
    
    const duration = updatedTrace.endTime.getTime() - updatedTrace.startTime.getTime();
    
    console.log(
      `%c🔷 Trace Ended: ${traceId}`,
      'color: #3b82f6; font-weight: bold',
      {
        duration: `${duration}ms`,
        spanCount: updatedTrace.spanCount,
        route: updatedTrace.route
      }
    );
  }
  
  // ─────────────────────────────────────────────────────────
  // Trace Access
  // ─────────────────────────────────────────────────────────
  
  /**
   * Get current active trace
   * 
   * @returns Current trace or null
   */
  getCurrentTrace(): Trace | null {
    return this._currentTrace();
  }
  
  /**
   * Get current trace ID
   * 
   * @returns Current trace ID or null
   */
  getCurrentTraceId(): string | null {
    return this._currentTrace()?.traceId || null;
  }
  
  /**
   * Increment span count for current trace
   */
  incrementSpanCount(): void {
    const currentTrace = this._currentTrace();
    if (!currentTrace) return;
    
    const updatedTrace: Trace = {
      ...currentTrace,
      spanCount: currentTrace.spanCount + 1
    };
    
    this._currentTrace.set(updatedTrace);
    
    // Update in history
    this._traces.update(traces =>
      traces.map(t => t.traceId === currentTrace.traceId ? updatedTrace : t)
    );
  }
  
  // ─────────────────────────────────────────────────────────
  // Utilities
  // ─────────────────────────────────────────────────────────
  
  /**
   * Generate unique trace ID
   * Format: trace-{timestamp}-{random}
   */
  private generateTraceId(): string {
    try {
      // Try native crypto UUID first
      if (typeof crypto !== 'undefined' && crypto.randomUUID) {
        return `trace-${crypto.randomUUID()}`;
      }
    } catch {
      // Fallback to timestamp + random
    }
    
    const timestamp = Date.now();
    const random = Math.random().toString(36).substring(2, 10);
    return `trace-${timestamp}-${random}`;
  }
  
  /**
   * Clear all traces (for testing/debugging)
   */
  clearTraces(): void {
    this._currentTrace.set(null);
    this._traces.set([]);
    console.log('%c🔷 All traces cleared', 'color: #3b82f6');
  }
  
  /**
   * Get trace statistics (for debugging)
   */
  getStats(): {
    totalTraces: number;
    activeTrace: boolean;
    totalSpans: number;
  } {
    const traces = this._traces();
    const totalSpans = traces.reduce((sum, t) => sum + t.spanCount, 0);
    
    return {
      totalTraces: traces.length,
      activeTrace: this.hasActiveTrace(),
      totalSpans
    };
  }
}

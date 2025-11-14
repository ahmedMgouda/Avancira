import { Injectable } from '@angular/core';

import { IdGenerator } from '../../utils/id-generator.utility';
import { CleanupManager } from '../../utils/cleanup-manager.utility';

/**
 * ═══════════════════════════════════════════════════════════════════════
 * TRACE SERVICE - FIXED
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * FIXES:
 * ✅ Automatic span cleanup (no memory leak)
 * ✅ TTL-based eviction (1 hour default)
 * ✅ Max size limit with LRU eviction
 * ✅ Uses CleanupManager utility
 */

export interface Span {
  spanId: string;
  name: string;
  startTime: Date;
  endTime?: Date;
  status: 'active' | 'ended' | 'error';
}

export interface TraceSnapshot  {
  traceId: string;
  activeSpan?: Span;
}

const DEFAULT_SPAN_TTL = 60 * 60 * 1000; // 1 hour
const DEFAULT_MAX_SPANS = 1000; // Max spans to keep
const CLEANUP_INTERVAL = 15 * 60 * 1000; // Cleanup every 15 minutes

@Injectable({ providedIn: 'root' })
export class TraceService {
  private currentTraceId: string | null = null;
  private spans = new Map<string, Span>();
  private spanTimestamps = new Map<string, number>();
  private cleanupManager: CleanupManager<string>;

  constructor() {
    // Initialize cleanup manager
    this.cleanupManager = new CleanupManager<string>({
      ttl: DEFAULT_SPAN_TTL,
      maxSize: DEFAULT_MAX_SPANS,
      cleanupInterval: CLEANUP_INTERVAL,
      onCleanup: (spanIds) => {
        spanIds.forEach(spanId => {
          this.spans.delete(spanId);
          this.spanTimestamps.delete(spanId);
        });
      }
    });
  }

  // ═══════════════════════════════════════════════════════════════════
  // Trace Management
  // ═══════════════════════════════════════════════════════════════════

  startTrace(): string {
    this.currentTraceId = IdGenerator.generateTraceId();
    return this.currentTraceId;
  }

  getCurrentTraceId(): string {
    if (!this.currentTraceId) {
      this.currentTraceId = IdGenerator.generateTraceId();
    }
    return this.currentTraceId;
  }

  endTrace(): void {
    this.currentTraceId = null;
    // Don't clear spans immediately - let cleanup manager handle it
  }

  hasActiveTrace(): boolean {
    return this.currentTraceId !== null;
  }

  // ═══════════════════════════════════════════════════════════════════
  // Span Management - WITH CLEANUP
  // ═══════════════════════════════════════════════════════════════════

  createSpan(name: string): Span {
    const span: Span = {
      spanId: IdGenerator.generateSpanId(),
      name,
      startTime: new Date(),
      status: 'active'
    };

    this.spans.set(span.spanId, span);
    this.spanTimestamps.set(span.spanId, Date.now());
    
    // Register with cleanup manager
    this.cleanupManager.add(span.spanId);

    return span;
  }

  endSpan(spanId: string, options?: { error?: Error }): void {
    const span = this.spans.get(spanId);

    if (span) {
      span.endTime = new Date();
      span.status = options?.error ? 'error' : 'ended';
      
      // Update timestamp for LRU
      this.spanTimestamps.set(spanId, Date.now());
      this.cleanupManager.touch(spanId);
    }
  }

  getSpan(spanId: string): Span | undefined {
    const span = this.spans.get(spanId);
    if (span) {
      // Touch for LRU
      this.cleanupManager.touch(spanId);
    }
    return span;
  }

  getAllSpans(): Span[] {
    return Array.from(this.spans.values());
  }

  getActiveSpan(): Span | undefined {
    return Array.from(this.spans.values()).find(s => s.status === 'active');
  }

  // ═══════════════════════════════════════════════════════════════════
  // Context Management
  // ═══════════════════════════════════════════════════════════════════

  getCurrentContext(): TraceSnapshot  {
    const traceId = this.getCurrentTraceId();
    const activeSpan = this.getActiveSpan();

    return {
      traceId,
      activeSpan
    };
  }

  clearSpans(): void {
    this.spans.clear();
    this.spanTimestamps.clear();
    this.cleanupManager.clear();
  }

  clearAll(): void {
    this.currentTraceId = null;
    this.clearSpans();
  }

  // ═══════════════════════════════════════════════════════════════════
  // Diagnostics
  // ═══════════════════════════════════════════════════════════════════

  getDiagnostics() {
    return {
      traceId: this.currentTraceId,
      activeSpans: Array.from(this.spans.values()).filter(s => s.status === 'active').length,
      totalSpans: this.spans.size,
      cleanup: this.cleanupManager.getStats()
    };
  }

  /**
   * Manual cleanup (for testing or forced cleanup)
   */
  forceCleanup(): void {
    this.cleanupManager.forceCleanup();
  }

  ngOnDestroy(): void {
    this.cleanupManager.destroy();
  }
}

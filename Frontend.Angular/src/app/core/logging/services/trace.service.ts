import { Injectable } from '@angular/core';

import { IdGenerator } from '../../utils/id-generator.utility';

/**
 * ═══════════════════════════════════════════════════════════════════════
 * TRACE SERVICE - FIXED
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * FIXES:
 * ✅ Custom cleanup implementation (CleanupManager not generic)
 * ✅ Automatic TTL-based span eviction
 * ✅ Max size limit with LRU eviction
 * ✅ No memory leaks
 */

export interface Span {
  spanId: string;
  name: string;
  startTime: Date;
  endTime?: Date;
  status: 'active' | 'ended' | 'error';
}

export interface TraceSnapshot {
  traceId: string;
  activeSpan?: Span;
}

const DEFAULT_SPAN_TTL = 60 * 60 * 1000; // 1 hour
const DEFAULT_MAX_SPANS = 1000;
const CLEANUP_INTERVAL = 15 * 60 * 1000; // 15 minutes

@Injectable({ providedIn: 'root' })
export class TraceService {
  private currentTraceId: string | null = null;
  private spans = new Map<string, Span>();
  private spanTimestamps = new Map<string, number>();
  private cleanupTimer?: ReturnType<typeof setInterval>;

  private readonly ttl = DEFAULT_SPAN_TTL;
  private readonly maxSize = DEFAULT_MAX_SPANS;

  constructor() {
    // Start cleanup timer
    this.cleanupTimer = setInterval(() => {
      this.cleanupExpiredSpans();
    }, CLEANUP_INTERVAL);
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
    
    // Check size limit
    if (this.spans.size > this.maxSize) {
      this.evictOldestSpan();
    }

    return span;
  }

  endSpan(spanId: string, options?: { error?: Error }): void {
    const span = this.spans.get(spanId);

    if (span) {
      span.endTime = new Date();
      span.status = options?.error ? 'error' : 'ended';
      this.spanTimestamps.set(spanId, Date.now());
    }
  }

  getSpan(spanId: string): Span | undefined {
    const span = this.spans.get(spanId);
    if (span) {
      // Touch for LRU
      this.spanTimestamps.set(spanId, Date.now());
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
  // Cleanup - FIX: Custom implementation
  // ═══════════════════════════════════════════════════════════════════

  private cleanupExpiredSpans(): void {
    const now = Date.now();
    const cutoff = now - this.ttl;
    const toDelete: string[] = [];

    for (const [spanId, timestamp] of this.spanTimestamps.entries()) {
      if (timestamp < cutoff) {
        toDelete.push(spanId);
      }
    }

    toDelete.forEach(spanId => {
      this.spans.delete(spanId);
      this.spanTimestamps.delete(spanId);
    });
  }

  private evictOldestSpan(): void {
    let oldestSpanId: string | null = null;
    let oldestTime = Infinity;

    for (const [spanId, timestamp] of this.spanTimestamps.entries()) {
      if (timestamp < oldestTime) {
        oldestTime = timestamp;
        oldestSpanId = spanId;
      }
    }

    if (oldestSpanId) {
      this.spans.delete(oldestSpanId);
      this.spanTimestamps.delete(oldestSpanId);
    }
  }

  // ═══════════════════════════════════════════════════════════════════
  // Context Management
  // ═══════════════════════════════════════════════════════════════════

  getCurrentContext(): TraceSnapshot {
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
  }

  clearAll(): void {
    this.currentTraceId = null;
    this.clearSpans();
  }

  // ═══════════════════════════════════════════════════════════════════
  // Diagnostics & Cleanup
  // ═══════════════════════════════════════════════════════════════════

  getDiagnostics() {
    return {
      traceId: this.currentTraceId,
      activeSpans: Array.from(this.spans.values()).filter(s => s.status === 'active').length,
      totalSpans: this.spans.size,
      maxSize: this.maxSize,
      ttl: this.ttl
    };
  }

  forceCleanup(): void {
    this.cleanupExpiredSpans();
  }

  ngOnDestroy(): void {
    if (this.cleanupTimer) {
      clearInterval(this.cleanupTimer);
    }
    this.clearAll();
  }
}

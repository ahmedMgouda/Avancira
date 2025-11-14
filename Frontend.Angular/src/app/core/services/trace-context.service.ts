import { Injectable } from '@angular/core';

import { type TraceSnapshot } from '../logging/services/trace.service';

import { IdGenerator } from '../utils/id-generator.utility';

/**
 * ═══════════════════════════════════════════════════════════════════════
 * TRACE CONTEXT SERVICE - FIXED
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * FIXES:
 * ✅ TraceContext type properly aligned with TraceSnapshot
 * ✅ Type guards for safe union type access
 * ✅ Removed unused parameters (flags, attempt)
 * ✅ Fixed union type access in createChildSpan
 * ✅ W3C traceparent format validation
 */

export interface TraceContext {
  traceId: string;
  spanId: string;
  parentSpanId?: string | null;
}

@Injectable({ providedIn: 'root' })
export class TraceContextService {
  private currentTraceId: string;
  private currentSpanId: string;

  // W3C Trace Context format regex
  private readonly TRACEPARENT_REGEX = /^([0-9a-f]{2})-([0-9a-f]{32})-([0-9a-f]{16})-([0-9a-f]{2})$/;
  private readonly TRACE_ID_ZERO = '00000000000000000000000000000000';
  private readonly SPAN_ID_ZERO = '0000000000000000';

  constructor() {
    this.currentTraceId = IdGenerator.generateTraceId();
    this.currentSpanId = IdGenerator.generateSpanId();
  }

  /**
   * Get current trace context as TraceContext (compatible with both types)
   */
  getCurrentContext(): TraceContext {
    return {
      traceId: this.currentTraceId,
      spanId: this.currentSpanId,
      parentSpanId: null
    };
  }

  /**
   * Get current trace snapshot (for TraceService compatibility)
   */
  getCurrentSnapshot(): TraceSnapshot {
    return {
      traceId: this.currentTraceId,
      activeSpan: {
        spanId: this.currentSpanId,
        name: 'current',
        startTime: new Date(),
        status: 'active'
      }
    };
  }

  /**
   * Create child span for retry/nested operations
   * FIX: Type guard to safely handle union types
   */
  createChildSpan(parentContext: TraceSnapshot | TraceContext): TraceContext {
    // Type guard: Check if it's a TraceSnapshot (has activeSpan property)
    const isTraceSnapshot = (ctx: TraceSnapshot | TraceContext): ctx is TraceSnapshot => {
      return 'activeSpan' in ctx;
    };

    // Extract spanId based on type
    const parentSpanId = isTraceSnapshot(parentContext)
      ? parentContext.activeSpan?.spanId
      : parentContext.spanId;

    return {
      traceId: parentContext.traceId,
      spanId: IdGenerator.generateSpanId(),
      parentSpanId: parentSpanId || null
    };
  }

  /**
   * Set trace and span IDs from external context
   */
  setContext(traceId: string, spanId: string): void {
    this.currentTraceId = traceId;
    this.currentSpanId = spanId;
  }

  /**
   * Start a new trace context
   */
  startNewTrace(): void {
    this.currentTraceId = IdGenerator.generateTraceId();
    this.currentSpanId = IdGenerator.generateSpanId();
  }

  /**
   * Parse W3C traceparent header with validation
   * Format: 00-traceId(32 hex)-spanId(16 hex)-flags(2 hex)
   * 
   * FIX: Removed unused 'flags' parameter with underscore prefix
   */
  parseTraceparent(traceparent: string): { traceId: string; spanId: string } | null {
    if (!traceparent || typeof traceparent !== 'string') {
      return null;
    }

    const trimmed = traceparent.trim().toLowerCase();
    const match = this.TRACEPARENT_REGEX.exec(trimmed);

    if (!match) {
      return null;
    }

    // FIX: Use underscore prefix for unused destructured values
    const [, version, traceId, spanId, _flags] = match;

    if (version !== '00') {
      return null;
    }

    if (traceId === this.TRACE_ID_ZERO || spanId === this.SPAN_ID_ZERO) {
      return null;
    }

    return { traceId, spanId };
  }

  /**
   * Generate W3C compliant traceparent header
   */
  generateTraceparent(): string {
    const traceId = this.currentTraceId.padEnd(32, '0').slice(0, 32);
    const spanId = this.currentSpanId.padEnd(16, '0').slice(0, 16);
    return `00-${traceId}-${spanId}-01`;
  }
}

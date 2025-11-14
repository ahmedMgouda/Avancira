import { Injectable } from '@angular/core';

import { type TraceSnapshot } from '../logging/services/trace.service';
import { IdGenerator } from '../utils/id-generator.utility';

/**
 * ═══════════════════════════════════════════════════════════════════════
 * TRACE CONTEXT SERVICE - FIXED
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * FIXES:
 * ✅ Added W3C traceparent format validation
 * ✅ Validates trace and span ID formats
 * ✅ Returns null on invalid input (graceful degradation)
 * 
 * W3C Trace Context spec:
 * version-traceId-spanId-flags
 * 00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01
 */

@Injectable({ providedIn: 'root' })
export class TraceContextService {
  private currentTraceId: string;
  private currentSpanId: string;

  // W3C Trace Context format regex
  // Format: version(2)-traceId(32)-spanId(16)-flags(2)
  private readonly TRACEPARENT_REGEX = /^([0-9a-f]{2})-([0-9a-f]{32})-([0-9a-f]{16})-([0-9a-f]{2})$/;
  private readonly TRACE_ID_ZERO = '00000000000000000000000000000000';
  private readonly SPAN_ID_ZERO = '0000000000000000';

  constructor() {
    this.currentTraceId = IdGenerator.generateTraceId();
    this.currentSpanId = IdGenerator.generateSpanId();
  }

  /**
   * Get current trace context
   */
  getCurrentContext(): TraceSnapshot {
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
   * Returns null if invalid format
   */
  parseTraceparent(traceparent: string): { traceId: string; spanId: string } | null {
    if (!traceparent || typeof traceparent !== 'string') {
      return null;
    }

    const trimmed = traceparent.trim().toLowerCase();
    const match = this.TRACEPARENT_REGEX.exec(trimmed);

    if (!match) {
      return null; // Invalid format
    }

    const [, version, traceId, spanId, flags] = match;

    // Validate version (currently only 00 is defined)
    if (version !== '00') {
      return null;
    }

    // Validate trace-id is not all zeros
    if (traceId === this.TRACE_ID_ZERO) {
      return null;
    }

    // Validate span-id is not all zeros
    if (spanId === this.SPAN_ID_ZERO) {
      return null;
    }

    return { traceId, spanId };
  }

  /**
   * Generate W3C compliant traceparent header
   * Format: 00-traceId(32)-spanId(16)-01
   */
  generateTraceparent(): string {
    const traceId = this.currentTraceId.padEnd(32, '0').slice(0, 32);
    const spanId = this.currentSpanId.padEnd(16, '0').slice(0, 16);
    return `00-${traceId}-${spanId}-01`;
  }
}

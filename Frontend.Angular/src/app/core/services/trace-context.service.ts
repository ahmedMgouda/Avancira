import { inject,Injectable } from '@angular/core';

import { SpanManagerService } from '../logging/services/span-manager.service';
import { TraceManagerService } from '../logging/services/trace-manager.service';

import { BrowserCompat } from '../logging/utils/browser-compat.util';

/**
 * W3C Trace Context Implementation (traceparent header)
 * Format: {version}-{trace-id}-{span-id}-{flags}
 * Example: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01
 */
export interface TraceContext {
  traceId: string;
  spanId: string;
  parentSpanId?: string;
  sampled: boolean;
  retryAttempt?: number;
}

@Injectable({ providedIn: 'root' })
export class TraceContextService {
  private readonly traceManager = inject(TraceManagerService);
  private readonly spanManager = inject(SpanManagerService);

  /**
   * Generate W3C traceparent header
   * @see https://www.w3.org/TR/trace-context/#traceparent-header
   */
  generateTraceparent(context?: Partial<TraceContext>): string {
    const version = '00';
    const traceId = context?.traceId || this.traceManager.getCurrentTraceId();
    const spanId = context?.spanId || this.generateSpanId();
    const flags = context?.sampled !== false ? '01' : '00';

    return `${version}-${traceId}-${spanId}-${flags}`;
  }

  /**
   * Generate tracestate header (for additional vendor data)
   */
  generateTracestate(retryAttempt?: number): string | null {
    if (retryAttempt !== undefined) {
      return `avancira=retry:${retryAttempt}`;
    }
    return null;
  }

  /**
   * Parse traceparent header
   */
  parseTraceparent(traceparent: string): TraceContext | null {
    const parts = traceparent.split('-');
    if (parts.length !== 4) return null;

    const [version, traceId, spanId, flags] = parts;
    if (version !== '00') return null;

    return {
      traceId,
      spanId,
      sampled: flags === '01'
    };
  }

  /**
   * Create child span context for retry
   */
  createChildSpan(parentContext: TraceContext, retryAttempt: number): TraceContext {
    return {
      traceId: parentContext.traceId, // Keep same trace ID
      spanId: this.generateSpanId(), // New span ID
      parentSpanId: parentContext.spanId, // Reference parent
      sampled: parentContext.sampled,
      retryAttempt
    };
  }

  /**
   * Generate 16-character hex span ID
   */
  private generateSpanId(): string {
    const uuid = BrowserCompat.generateUUID().replace(/-/g, '');
    return uuid.substring(0, 16);
  }

  /**
   * Get current trace context from active span
   */
  getCurrentContext(): TraceContext {
    const traceId = this.traceManager.getCurrentTraceId();
    const activeSpans = this.spanManager.getAllSpans().filter(s => s.status === 'active');
    const activeSpan = activeSpans[activeSpans.length - 1]; // Most recent

    return {
      traceId,
      spanId: activeSpan?.spanId || this.generateSpanId(),
      sampled: true
    };
  }
}
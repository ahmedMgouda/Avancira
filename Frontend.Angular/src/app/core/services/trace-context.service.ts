import { inject, Injectable } from '@angular/core';

import { TraceService } from '../logging/services/trace.service';

import { IdGenerator } from '../utils/id-generator.utility';

/**
 * W3C Trace Context Implementation
 * Format: 00-{trace-id}-{span-id}-{flags}
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
  private readonly traceService = inject(TraceService);

  /** Generate W3C traceparent header */
  generateTraceparent(context?: Partial<TraceContext>): string {
    const version = '00';
    const traceId = context?.traceId || this.traceService.getCurrentTraceId() || IdGenerator.generateTraceId();
    const spanId = context?.spanId || IdGenerator.generateSpanId();
    const flags = context?.sampled !== false ? '01' : '00';

    return `${version}-${traceId}-${spanId}-${flags}`;
  }

  /** Generate tracestate header (vendor-specific metadata) */
  generateTracestate(retryAttempt?: number): string | null {
    return retryAttempt !== undefined ? `avancira=retry:${retryAttempt}` : null;
  }

  /** Parse incoming traceparent header */
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

  /** Create a child span (for retries, nested calls, etc.) */
  createChildSpan(parentContext: TraceContext, retryAttempt?: number): TraceContext {
    return {
      traceId: parentContext.traceId,
      spanId: IdGenerator.generateSpanId(),
      parentSpanId: parentContext.spanId,
      sampled: parentContext.sampled,
      retryAttempt
    };
  }

  /** Get the currently active trace context (or create one) */
  getCurrentContext(): TraceContext {
    const traceId =
      this.traceService.getCurrentTraceId() || IdGenerator.generateTraceId();
    const activeSpans = this.traceService.getAllSpans().filter(s => s.status === 'active');
    const activeSpan = activeSpans.at(-1);

    return {
      traceId,
      spanId: activeSpan?.spanId || IdGenerator.generateSpanId(),
      sampled: true
    };
  }
}

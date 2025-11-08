import { Injectable } from '@angular/core';

import { Span } from '../models/span.model';
import { IdGenerator } from '../../utils/id-generator';

@Injectable({ providedIn: 'root' })
export class SpanManagerService {
  private spans = new Map<string, Span>();
  
  createSpan(name: string): Span {
    const span: Span = {
      spanId: IdGenerator.generateSpanId(),
      name,
      startTime: new Date(),
      status: 'active'
    };
    
    this.spans.set(span.spanId, span);
    return span;
  }
  
  endSpan(spanId: string, options?: { error?: Error }): void {
    const span = this.spans.get(spanId);
    
    if (span) {
      span.endTime = new Date();
      span.status = options?.error ? 'error' : 'ended';
    }
  }
  
  getSpan(spanId: string): Span | undefined {
    return this.spans.get(spanId);
  }
  
  getAllSpans(): Span[] {
    return Array.from(this.spans.values());
  }
  
  clearAllSpans(): void {
    this.spans.clear();
  }
}

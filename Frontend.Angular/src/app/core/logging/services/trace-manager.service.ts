import { Injectable } from '@angular/core';

import { IdGenerator } from '../utils/id-generator.util';

@Injectable({ providedIn: 'root' })
export class TraceManagerService {
  private currentTraceId: string | null = null;
  
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
}

// core/logging/services/navigation-trace.service.ts
/**
 * Navigation Trace Service - UPDATED
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * CHANGES:
 * ✅ Uses TraceService (merged service)
 */

import { inject, Injectable } from '@angular/core';
import { NavigationStart, Router } from '@angular/router';
import { filter } from 'rxjs/operators';

import { TraceService } from './trace.service';

@Injectable({ providedIn: 'root' })
export class NavigationTraceService {
  private readonly router = inject(Router);
  private readonly traceService = inject(TraceService);

  initialize(): void {
    this.router.events.pipe(
      filter(event => event instanceof NavigationStart)
    ).subscribe(() => {
      this.endCurrentTrace();
      this.startNewTrace();
    });
  }

  private startNewTrace(): void {
    this.traceService.startTrace();
  }

  private endCurrentTrace(): void {
    this.traceService.clearAll();
  }
}
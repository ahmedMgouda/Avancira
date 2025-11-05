import { inject,Injectable } from '@angular/core';
import { NavigationStart,Router } from '@angular/router';
import { filter } from 'rxjs/operators';

import { SpanManagerService } from './span-manager.service';
import { TraceManagerService } from './trace-manager.service';

@Injectable({ providedIn: 'root' })
export class NavigationTraceService {
  private readonly router = inject(Router);
  private readonly traceManager = inject(TraceManagerService);
  private readonly spanManager = inject(SpanManagerService);
  
  initialize(): void {
    this.router.events.pipe(
      filter(event => event instanceof NavigationStart)
    ).subscribe(() => {
      this.endCurrentTrace();
      this.startNewTrace();
    });
  }
  
  private startNewTrace(): void {
    this.traceManager.startTrace();
  }
  
  private endCurrentTrace(): void {
    this.spanManager.clearAllSpans();
    this.traceManager.endTrace();
  }
}

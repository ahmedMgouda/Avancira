import { Injectable, inject } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { LoggerService } from '../services/logger.service';
import { SpanManagerService } from '../services/span-manager.service';

@Injectable()
export class HttpLoggingInterceptor implements HttpInterceptor {
  private readonly logger = inject(LoggerService);
  private readonly spanManager = inject(SpanManagerService);
  
  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const startTime = Date.now();
    const span = this.spanManager.createSpan(req.url);
    
    return next.handle(req).pipe(
      tap({
        next: (event) => {
          if (event instanceof HttpResponse) {
            const duration = Date.now() - startTime;
            this.spanManager.endSpan(span.spanId);
            
            this.logger.info(`${event.status} ${req.method} ${req.url}`, {
              log: {
                source: 'HTTP',
                type: 'http'
              },
              http: {
                method: req.method,
                url: req.url,
                status_code: event.status,
                duration_ms: duration
              }
            });
          }
        },
        error: (error) => {
          this.spanManager.endSpan(span.spanId, { error });
        }
      })
    );
  }
}

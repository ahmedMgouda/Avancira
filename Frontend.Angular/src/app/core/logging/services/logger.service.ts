/**
 * Logger Service - FIXED
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * FIXES:
 * ✅ Added platform checks for beforeunload
 * ✅ SSR-safe with proper guards
 * ✅ Supports custom data in logs (already implemented)
 */

import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable, Injector, PLATFORM_ID, signal } from '@angular/core';
import { catchError, throwError } from 'rxjs';

import { AuthService } from '../../auth/services/auth.service';
import { ResilienceService } from '../../http/services/resilience.service';
import { TraceService } from './trace.service';
import { LogMonitorService } from './log-monitor.service';

import { environment } from '../../../environments/environment';
import { getDeduplicationConfig } from '../../config/deduplication.config';
import { getLoggingConfig } from '../../config/logging.config';
import { getSamplingConfig } from '../../config/sampling.config';
import { BufferManager } from '../../utils/buffer-manager.utility';
import { DedupManager } from '../../utils/dedup-manager.utility';
import { IdGenerator } from '../../utils/id-generator.utility';
import { SamplingManager } from '../../utils/sampling-manager.utility';
import { BaseLogEntry, LogContext } from '../models/base-log-entry.model';
import { LogLevel } from '../models/log-level.model';
import { DataSanitizer } from '../utils/data-sanitizer.utility';
import { SourceExtractor } from '../utils/source-extractor.utility';

@Injectable({ providedIn: 'root' })
export class LoggerService {
  private readonly traceService = inject(TraceService);
  private readonly logMonitor = inject(LogMonitorService);
  private readonly http = inject(HttpClient);
  private readonly injector = inject(Injector);
  private readonly resilience = inject(ResilienceService);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  private readonly config = getLoggingConfig();

  private readonly dedup = new DedupManager<BaseLogEntry>({
    ...getDeduplicationConfig().logging,
    hashFn: (log) => `${log.log.level}-${log.log.message}-${log.log.type}`
  });

  private readonly sampling = new SamplingManager(getSamplingConfig());

  private readonly buffer = new BufferManager<BaseLogEntry>({
    maxSize: this.config.buffer.maxSize,
    onOverflow: (count) => {
      if (!environment.production) {
        console.warn(`[Logger] Buffer overflow: dropped ${count} log(s)`);
      }
    }
  });

  private readonly sanitizer = new DataSanitizer(
    this.config.sanitization.sensitiveFields,
    this.config.sanitization.redactedValue
  );

  private authService: AuthService | null = null;
  private anonymousSessionId: string | null = null;
  private flushTimer?: ReturnType<typeof setInterval>;

  readonly bufferSize = signal(0);

  constructor() {
    this.initializeAnonymousSession();
    this.initializeAuthServiceConnection();
    this.initializeAutoFlush();
    this.setupUnloadHandler();
  }

  // ═══════════════════════════════════════════════════════════════════
  // Public Logging API
  // ═══════════════════════════════════════════════════════════════════

  trace(message: string, context?: LogContext): void {
    this.log(LogLevel.TRACE, message, context);
  }

  debug(message: string, context?: LogContext): void {
    this.log(LogLevel.DEBUG, message, context);
  }

  info(message: string, context?: LogContext): void {
    this.log(LogLevel.INFO, message, context);
  }

  warn(message: string, context?: LogContext): void {
    this.log(LogLevel.WARN, message, context);
  }

  error(message: string, error?: unknown, context?: LogContext): void {
    this.log(LogLevel.ERROR, message, context, error);
  }

  fatal(message: string, error?: unknown, context?: LogContext): void {
    this.log(LogLevel.FATAL, message, context, error);
  }

  // ═══════════════════════════════════════════════════════════════════
  // Core Logging Logic
  // ═══════════════════════════════════════════════════════════════════

  private log(
    level: LogLevel,
    message: string,
    context?: LogContext,
    error?: unknown
  ): void {
    const logType = context?.log?.type || 'application';

    if (!this.sampling.shouldSample(logType)) {
      this.sampling.recordDecision(logType, false);
      return;
    }

    this.sampling.recordDecision(logType, true);

    const entry = this.createBaseEntry(level, message, context, error);

    if (this.dedup.check(entry)) {
      return;
    }

    if (this.config.console.enabled) {
      this.logToConsole(entry);
    }

    this.logMonitor.broadcast(entry);

    this.buffer.add(entry);
    this.bufferSize.set(this.buffer.size());

    if (this.buffer.size() >= this.config.remote.batchSize) {
      this.flush();
    }
  }

  private createBaseEntry(
    level: LogLevel,
    message: string,
    context?: LogContext,
    _error?: unknown
  ): BaseLogEntry {
    const now = new Date();
    const traceContext = this.traceService.getCurrentContext();
    const source = context?.log?.source || SourceExtractor.extract(5);
    const type = context?.log?.type || 'application';

    const entry: BaseLogEntry = {
      '@timestamp': now.toISOString(),
      '@version': '1.0',

      log: {
        id: context?.log?.id || IdGenerator.generateLogId(),
        level: this.getLevelName(level),
        source,
        type,
        message
      },

      service: {
        name: this.config.application.name,
        version: this.config.application.version,
        environment: environment.production ? 'production' : 'development'
      },

      trace: {
        id: traceContext.traceId,
        span: traceContext.activeSpan ? {
          id: traceContext.activeSpan.spanId,
          name: traceContext.activeSpan.name,
          duration_ms: traceContext.activeSpan.endTime
            ? traceContext.activeSpan.endTime.getTime() - traceContext.activeSpan.startTime.getTime()
            : undefined
        } : undefined
      },

      client: {
        url: this.isBrowser ? window.location.href : 'ssr',
        route: this.isBrowser ? window.location.pathname : '/',
        user_agent: this.isBrowser ? navigator.userAgent : 'SSR'
      }
    };

    const sessionId = this.getSessionId();
    const userId = this.getUserId();

    if (sessionId) {
      entry.session = {
        id: sessionId,
        user: userId ? { id: userId } : undefined
      };
    }

    if (context?.http) {
      entry.http = this.sanitizeHttpData(context.http);
    }

    if (context?.error) {
      entry.error = context.error;
    }

    if (context?.navigation) {
      entry.navigation = context.navigation;
    }

    // Add custom fields
    if (context) {
      const standardFields = new Set([
        '@timestamp', '@version', 'log', 'service', 'trace', 
        'client', 'session', 'http', 'error', 'navigation'
      ]);

      Object.keys(context).forEach(key => {
        if (!standardFields.has(key)) {
          entry[key] = context[key];
        }
      });
    }

    return entry;
  }

  // ═══════════════════════════════════════════════════════════════════
  // Remote Transmission
  // ═══════════════════════════════════════════════════════════════════

  flush(): void {
    if (!this.config.remote.enabled) {
      return;
    }

    const logs = this.buffer.flush();
    if (logs.length === 0) {
      return;
    }

    this.bufferSize.set(0);

    const headers = {
      'X-Skip-Logging': 'true',
      'X-Skip-Retry': 'true',
      'X-Skip-Loading': 'true'
    };

    this.http.post(
      this.config.remote.endpoint,
      { logs },
      { headers, withCredentials: true }
    )
    .pipe(
      this.resilience.withRetry({ maxRetries: 0 }),
      catchError((error) => {
        if (!environment.production) {
          console.error('[Logger] Failed to send logs:', error);
        }
        return throwError(() => error);
      })
    )
    .subscribe();
  }

  // ═══════════════════════════════════════════════════════════════════
  // Initialization - FIXED
  // ═══════════════════════════════════════════════════════════════════

  private initializeAnonymousSession(): void {
    this.anonymousSessionId = `anon-${IdGenerator.generateUUID()}`;
  }

  private initializeAuthServiceConnection(): void {
    try {
      this.authService = this.injector.get(AuthService, null, { optional: true });
    } catch {
      // AuthService not available
    }
  }

  private initializeAutoFlush(): void {
    if (!this.config.remote.enabled) {
      return;
    }

    this.flushTimer = setInterval(() => {
      this.flush();
    }, this.config.remote.flushInterval);
  }

  private setupUnloadHandler(): void {
    // FIX: Guard with platform check
    if (!this.isBrowser || !this.config.remote.enabled) {
      return;
    }

    window.addEventListener('beforeunload', () => {
      const logs = this.buffer.peek();

      if (logs.length > 0 && navigator.sendBeacon) {
        navigator.sendBeacon(
          this.config.remote.endpoint,
          JSON.stringify({ logs })
        );
      }
    });
  }

  // ═══════════════════════════════════════════════════════════════════
  // Helper Methods
  // ═══════════════════════════════════════════════════════════════════

  private getSessionId(): string {
    return this.authService?.sessionId?.() || this.anonymousSessionId!;
  }

  private getUserId(): string | null {
    return this.authService?.userId?.() || null;
  }

  private sanitizeHttpData(http: BaseLogEntry['http']): BaseLogEntry['http'] {
    if (!http || !this.config.sanitization.enabled) {
      return http;
    }

    return {
      ...http,
      problem_details: http.problem_details ? { ...http.problem_details } : undefined
    };
  }

  private logToConsole(entry: BaseLogEntry): void {
    const level = entry.log.level || 'INFO';
    const style = this.getConsoleStyle(level);
    const prefix = `[${level}] [${entry.log.source}]`;

    const { 
      '@timestamp': timestamp, 
      '@version': version, 
      log, 
      service, 
      trace, 
      client, 
      session,
      ...customFields 
    } = entry;

    console.log(
      `%c${prefix}%c ${entry.log.message}`,
      style,
      'color: inherit',
      {
        trace_id: trace.id,
        span_id: trace.span?.id,
        type: log.type,
        ...entry.http,
        ...entry.error,
        ...entry.navigation,
        ...(Object.keys(customFields).length > 0 ? { custom: customFields } : {})
      }
    );
  }

  private getConsoleStyle(level: string): string {
    if (!this.config.console.useColors) {
      return '';
    }

    const styles: Record<string, string> = {
      TRACE: 'color: #6b7280',
      DEBUG: 'color: #3b82f6',
      INFO: 'color: #10b981',
      WARN: 'color: #f59e0b; font-weight: bold',
      ERROR: 'color: #ef4444; font-weight: bold',
      FATAL: 'color: #7e22ce; font-weight: bold'
    };

    return styles[level] || '';
  }

  private getLevelName(level: LogLevel): string {
    const names = ['TRACE', 'DEBUG', 'INFO', 'WARN', 'ERROR', 'FATAL'];
    return names[level] || 'UNKNOWN';
  }

  // ═══════════════════════════════════════════════════════════════════
  // Diagnostics
  // ═══════════════════════════════════════════════════════════════════

  getDiagnostics() {
    return {
      buffer: this.buffer.getDiagnostics(),
      deduplication: this.dedup.getStats(),
      sampling: this.sampling.getStats(),
      config: {
        console: this.config.console.enabled,
        remote: this.config.remote.enabled,
        batchSize: this.config.remote.batchSize
      }
    };
  }

  ngOnDestroy(): void {
    if (this.flushTimer) {
      clearInterval(this.flushTimer);
    }
    this.flush();
    this.dedup.destroy();
  }
}

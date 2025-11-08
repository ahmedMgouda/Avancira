import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, Injector, OnDestroy, signal } from '@angular/core';
import { catchError, throwError } from 'rxjs';

import { AuthService } from '../../auth/services/auth.service';
import { ResilienceService } from '../../http/services/resilience.service';
import { LogMonitorService } from './log-monitor.service';
import { SpanManagerService } from './span-manager.service';
import { TraceManagerService } from './trace-manager.service';

import { environment } from '../../../environments/environment';
import { IdGenerator } from '../../utils/id-generator';
import { getLoggingConfig, UserTypeLoggingConfig } from '../config/logging.config';
import { BaseLogEntry } from '../models/base-log-entry.model';
import { LogLevel } from '../models/log-level.model';
import { BrowserCompat } from '../utils/browser-compat.util';
import { DataSanitizer } from '../utils/data-sanitizer.util';
import { SourceExtractor } from '../utils/source-extractor.util';

@Injectable({ providedIn: 'root' })
export class LoggerService implements OnDestroy {
  private readonly traceManager = inject(TraceManagerService);
  private readonly spanManager = inject(SpanManagerService);
  private readonly monitorService = inject(LogMonitorService);
  private readonly http = inject(HttpClient);
  private readonly injector = inject(Injector);
  private readonly resilience = inject(ResilienceService);
  
  private readonly config = getLoggingConfig();
  private readonly sanitizer = new DataSanitizer(
    this.config.sanitization.sensitiveFields,
    this.config.sanitization.redactedValue
  );
  
  private readonly logBuffer = signal<BaseLogEntry[]>([]);
  private authService: AuthService | null = null;
  private anonymousSessionId: string | null = null;
  private flushTimer?: ReturnType<typeof setInterval>;
  private bufferWarningShown = false;
  
  // Deduplication cache
  private recentLogHashes = new Map<string, number>();
  
  readonly bufferSize = computed(() => this.logBuffer().length);
  readonly bufferUsagePercent = computed(() => {
    const userConfig = this.getCurrentUserConfig();
    return (this.logBuffer().length / userConfig.maxBufferSize) * 100;
  });
  
  constructor() {
    this.logBrowserCompatibility();
    this.initializeAnonymousSession();
    this.initializeAuthServiceConnection();
    this.initializeAutoFlush();
    this.setupUnloadHandler();
  }
  
  // ────────────────────────────────────────────────────────────────
  // PUBLIC LOGGING METHODS
  // ────────────────────────────────────────────────────────────────
  
  trace(message: string, context?: Partial<BaseLogEntry>): void {
    this.log(LogLevel.TRACE, message, context);
  }
  
  debug(message: string, context?: Partial<BaseLogEntry>): void {
    this.log(LogLevel.DEBUG, message, context);
  }
  
  info(message: string, context?: Partial<BaseLogEntry>): void {
    this.log(LogLevel.INFO, message, context);
  }
  
  warn(message: string, context?: Partial<BaseLogEntry>): void {
    this.log(LogLevel.WARN, message, context);
  }
  
  error(message: string, error?: unknown, context?: Partial<BaseLogEntry>): void {
    this.log(LogLevel.ERROR, message, context, error);
  }
  
  fatal(message: string, error?: unknown, context?: Partial<BaseLogEntry>): void {
    this.log(LogLevel.FATAL, message, context, error);
  }
  
  // ────────────────────────────────────────────────────────────────
  // INITIALIZATION
  // ────────────────────────────────────────────────────────────────
  
  private logBrowserCompatibility(): void {
    if (!environment.production) {
      const info = BrowserCompat.getBrowserInfo();
      console.log('[Logger] Browser Compatibility:', {
        uuid: info.hasUUIDSupport ? '✅' : '⚠️ Fallback',
        beacon: info.hasBeaconSupport ? '✅' : '⚠️ Disabled',
        sessionStorage: info.hasSessionStorage ? '✅' : '⚠️ Disabled',
        userAgent: info.userAgent,
        platform: info.platform
      });
    }
  }
  
  private initializeAnonymousSession(): void {
    try {
      let sessionId = BrowserCompat.getSessionItem('anon-session-id');
      
      if (!sessionId) {
        sessionId = `anon-${BrowserCompat.generateUUID()}`;
        BrowserCompat.setSessionItem('anon-session-id', sessionId);
      }
      
      this.anonymousSessionId = sessionId;
    } catch {
      this.anonymousSessionId = `anon-${Date.now()}-${Math.random().toString(36).substring(2)}`;
    }
  }
  
  private initializeAuthServiceConnection(): void {
    try {
      this.authService = this.injector.get(AuthService, null, { optional: true });
      
      if (!this.authService && !environment.production) {
        console.warn('[Logger] AuthService not available for session tracking');
      }
    } catch (error) {
      if (!environment.production) {
        console.warn('[Logger] Failed to inject AuthService:', error);
      }
    }
  }
  
  // ────────────────────────────────────────────────────────────────
  // USER CONFIG SELECTION
  // ────────────────────────────────────────────────────────────────
  
  private getCurrentUserConfig(): UserTypeLoggingConfig {
    return this.isAuthenticated() 
      ? this.config.authenticated 
      : this.config.anonymous;
  }
  
  private isAuthenticated(): boolean {
    return !!this.getUserId();
  }
  
  // ────────────────────────────────────────────────────────────────
  // CORE LOGGING
  // ────────────────────────────────────────────────────────────────
  
  private log(
    level: LogLevel,
    message: string,
    context?: Partial<BaseLogEntry>,
    error?: unknown
  ): void {
    const userConfig = this.getCurrentUserConfig();
    
    // Check if logging is enabled for this user type
    if (!userConfig.enabled) {
      return;
    }
    
    // Check log level threshold
    if (level < userConfig.minLevel) {
      return;
    }
    
    // Apply sampling (probabilistic)
    if (Math.random() > userConfig.samplingRate) {
      return;
    }
    
    // Check action whitelist
    const logType = context?.log?.type || 'application';
    if (userConfig.logActions[0] !== '*') {
      if (!userConfig.logActions.includes(logType)) {
        return;
      }
    }
    
    const entry = this.createBaseEntry(level, message, context, error, userConfig);
    
    // Deduplication check
    if (this.config.deduplication.enabled && this.isDuplicate(entry)) {
      if (!environment.production) {
        console.warn('[Logger] Duplicate log suppressed:', message);
      }
      return;
    }
    
    if (this.config.console.enabled) {
      this.logToConsole(entry);
    }
    
    this.monitorService.broadcast(entry);
    
    this.addToBuffer(entry);
  }
  
  private createBaseEntry(
    level: LogLevel,
    message: string,
    context?: Partial<BaseLogEntry>,
    _error?: unknown,
    userConfig?: UserTypeLoggingConfig
  ): BaseLogEntry {
    const now = new Date();
    const traceId = this.traceManager.getCurrentTraceId();
    
    const source = context?.log?.source || SourceExtractor.extract(5);
    const type = context?.log?.type || 'application';
    
    const currentSpan = this.getCurrentSpan(context);
    
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
        id: traceId,
        span: currentSpan
      },
      
      client: {
        url: window.location.href,
        route: window.location.pathname,
        user_agent: navigator.userAgent
      }
    };
    
    // Only include session/user data if allowed by config
    const includeUserData = userConfig?.includeUserData ?? true;
    
    if (includeUserData) {
      const sessionId = this.getSessionId();
      const userId = this.getUserId();
      
      if (sessionId) {
        entry.session = {
          id: sessionId,
          user: userId ? { id: userId } : undefined as any
        };
        
        if (!userId) {
          delete (entry.session as any).user;
        }
      }
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
    
    return entry;
  }
  
  private getCurrentSpan(context?: Partial<BaseLogEntry>): BaseLogEntry['trace']['span'] {
    if (context?.log?.type === 'http') {
      const allSpans = this.spanManager.getAllSpans();
      const activeSpan = allSpans.find(s => s.status === 'active');
      
      if (activeSpan) {
        return {
          id: activeSpan.spanId,
          name: activeSpan.name,
          duration_ms: activeSpan.endTime 
            ? activeSpan.endTime.getTime() - activeSpan.startTime.getTime()
            : undefined
        };
      }
    }
    
    return undefined;
  }
  
  private getSessionId(): string {
    const authSessionId = this.authService?.sessionId?.();
    if (authSessionId) return authSessionId;
    
    return this.anonymousSessionId!;
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
      problem_details: http.problem_details ? {
        ...http.problem_details
      } : undefined
    };
  }
  
  // ────────────────────────────────────────────────────────────────
  // DEDUPLICATION
  // ────────────────────────────────────────────────────────────────
  
  private isDuplicate(entry: BaseLogEntry): boolean {
    const hash = this.hashLog(entry);
    const lastTime = this.recentLogHashes.get(hash);
    
    if (lastTime && Date.now() - lastTime < this.config.deduplication.timeWindowMs) {
      return true;
    }
    
    this.recentLogHashes.set(hash, Date.now());
    this.cleanupLogHashes();
    
    return false;
  }
  
  private hashLog(entry: BaseLogEntry): string {
    // Create hash from level + message + type
    return `${entry.log.level}-${entry.log.message}-${entry.log.type}`;
  }
  
  private cleanupLogHashes(): void {
    if (this.recentLogHashes.size <= this.config.deduplication.maxCacheSize) {
      return;
    }
    
    // Remove oldest entries
    const entries = Array.from(this.recentLogHashes.entries())
      .sort((a, b) => a[1] - b[1]);
    
    const toRemove = entries.slice(0, entries.length - this.config.deduplication.maxCacheSize);
    toRemove.forEach(([key]) => this.recentLogHashes.delete(key));
  }
  
  // ────────────────────────────────────────────────────────────────
  // BUFFER MANAGEMENT
  // ────────────────────────────────────────────────────────────────
  
  private addToBuffer(entry: BaseLogEntry): void {
    const userConfig = this.getCurrentUserConfig();
    
    this.logBuffer.update(buffer => {
      const newBuffer = [...buffer, entry];
      
      // Check for buffer overflow warning
      this.checkBufferWarning(newBuffer.length, userConfig.maxBufferSize);
      
      // Auto-flush if batch size reached
      if (newBuffer.length >= this.config.remote.batchSize) {
        this.flush();
        return [];
      }
      
      // Drop oldest logs if max buffer exceeded
      if (newBuffer.length > userConfig.maxBufferSize) {
        const dropped = newBuffer.length - userConfig.maxBufferSize;
        console.warn(
          `[Logger] Buffer overflow: dropped ${dropped} oldest log(s). ` +
          `Consider increasing maxBufferSize or decreasing flushInterval.`
        );
        return newBuffer.slice(-userConfig.maxBufferSize);
      }
      
      return newBuffer;
    });
  }
  
  private checkBufferWarning(bufferSize: number, maxBufferSize: number): void {
    const threshold = maxBufferSize * this.config.bufferWarningThreshold;
    
    if (bufferSize >= threshold && !this.bufferWarningShown) {
      console.warn(
        `[Logger] Buffer usage at ${Math.round((bufferSize / maxBufferSize) * 100)}% ` +
        `(${bufferSize}/${maxBufferSize}). ` +
        `Consider flushing more frequently or increasing buffer size.`
      );
      this.bufferWarningShown = true;
    } else if (bufferSize < threshold / 2) {
      this.bufferWarningShown = false;
    }
  }
  
  // ────────────────────────────────────────────────────────────────
  // REMOTE TRANSMISSION (with skip headers)
  // ────────────────────────────────────────────────────────────────
  
  private flush(): void {
    if (!this.config.remote.enabled) {
      return;
    }
    
    let buffer = this.logBuffer();
    if (buffer.length === 0) {
      return;
    }
    
    this.logBuffer.set([]);
    
    // Create headers to skip all interceptors for logging endpoint
    const headers = {
      'X-Skip-Logging': 'true',
      'X-Skip-Retry': 'true',
      'X-Skip-Loading': 'true'
    };
    
    const retryConfig = this.config.remote.retry.enabled
      ? {
          maxRetries: this.config.remote.retry.maxRetries,
          scalingDuration: this.config.remote.retry.baseDelayMs,
          maxDelay: this.config.remote.retry.maxDelayMs,
          excludedStatusCodes: [400, 401, 403, 404, 422]
        }
      : { maxRetries: 0, scalingDuration: 0, excludedStatusCodes: [] };
    
    this.http.post(
      this.config.remote.endpoint,
      { logs: buffer },
      { headers, withCredentials: true }
    )
      .pipe(
        this.resilience.withRetry(retryConfig),
        catchError((error) => {
          // DON'T LOG THIS ERROR (would create loop)
          console.error('[Logger] Failed to send logs after retries:', error);
          // TODO Phase 2: Store in IndexedDB
          return throwError(() => error);
        })
      )
      .subscribe({
        next: () => {
          if (!environment.production) {
            console.log(`[Logger] Successfully sent ${buffer.length} log(s)`);
          }
        }
      });
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
    if (!this.config.remote.enabled) {
      return;
    }
    
    window.addEventListener('beforeunload', () => {
      const buffer = this.logBuffer();
      
      if (buffer.length > 0 && BrowserCompat.hasSendBeaconSupport()) {
        navigator.sendBeacon(
          this.config.remote.endpoint,
          JSON.stringify({ logs: buffer })
        );
      }
    });
  }
  
  // ────────────────────────────────────────────────────────────────
  // CONSOLE LOGGING
  // ────────────────────────────────────────────────────────────────
  
  private logToConsole(entry: BaseLogEntry): void {
    const level = entry.log.level || 'INFO';
    const style = this.getConsoleStyle(level);
    const prefix = `[${entry.log.level}] [${entry.log.source}]`;
    
    console.log(
      `%c${prefix}%c ${entry.log.message}`,
      style,
      'color: inherit',
      {
        trace_id: entry.trace.id,
        span_id: entry.trace.span?.id,
        type: entry.log.type,
        ...entry.http,
        ...entry.error,
        ...entry.navigation
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
  
  ngOnDestroy(): void {
    if (this.flushTimer) {
      clearInterval(this.flushTimer);
    }
    this.flush();
  }
}
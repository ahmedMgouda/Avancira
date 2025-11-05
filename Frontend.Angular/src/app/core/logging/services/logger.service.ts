import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, Injector,OnDestroy, signal } from '@angular/core';

import { LogMonitorService } from './log-monitor.service';
import { SpanManagerService } from './span-manager.service';
import { TraceManagerService } from './trace-manager.service';

import { getLoggingConfig } from '../config/logging.config';
import { BaseLogEntry } from '../models/base-log-entry.model';
import { LogLevel } from '../models/log-level.model';
import { DataSanitizer } from '../utils/data-sanitizer.util';
import { IdGenerator } from '../utils/id-generator.util';
import { SourceExtractor } from '../utils/source-extractor.util';

@Injectable({ providedIn: 'root' })
export class LoggerService implements OnDestroy {
  private readonly traceManager = inject(TraceManagerService);
  private readonly spanManager = inject(SpanManagerService);
  private readonly monitorService = inject(LogMonitorService);
  private readonly http = inject(HttpClient);
  private readonly injector = inject(Injector);
  
  private readonly config = getLoggingConfig(this.isProduction());
  private readonly sanitizer = new DataSanitizer(
    this.config.sanitization.sensitiveFields,
    this.config.sanitization.redactedValue
  );
  
  private readonly logBuffer = signal<BaseLogEntry[]>([]);
  private sessionIdSignal: (() => string | null) | null = null;
  private userIdSignal: (() => string | null) | null = null;
  private anonymousSessionId: string | null = null;
  private flushTimer?: ReturnType<typeof setInterval>;
  
  readonly bufferSize = computed(() => this.logBuffer().length);
  
  constructor() {
    this.initializeAnonymousSession();
    this.initializeAuthServiceConnection();
    this.initializeAutoFlush();
    this.setupUnloadHandler();
  }
  
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
  
  private initializeAnonymousSession(): void {
    try {
      let sessionId = sessionStorage.getItem('anon-session-id');
      
      if (!sessionId) {
        sessionId = `anon-${crypto.randomUUID()}`;
        sessionStorage.setItem('anon-session-id', sessionId);
      }
      
      this.anonymousSessionId = sessionId;
    } catch {
      this.anonymousSessionId = `anon-${Date.now()}-${Math.random().toString(36).substring(2)}`;
    }
  }
  
  private initializeAuthServiceConnection(): void {
    try {
      const AuthService = this.injector.get('AuthService' as any, null);
      if (AuthService && AuthService.sessionId) {
        this.sessionIdSignal = () => AuthService.sessionId();
      }
      if (AuthService && AuthService.userId) {
        this.userIdSignal = () => AuthService.userId();
      }
    } catch {
    }
  }
  
  private log(
    level: LogLevel,
    message: string,
    context?: Partial<BaseLogEntry>,
    error?: unknown
  ): void {
    if (!this.config.enabled || level < this.config.minLevel) {
      return;
    }
    
    const entry = this.createBaseEntry(level, message, context, error);
    
    if (this.config.console.enabled) {
      console.log("enable logging...");
      this.logToConsole(entry);
    }
    
    this.monitorService.broadcast(entry);
    
    this.addToBuffer(entry);
  }
  
  private createBaseEntry(
    level: LogLevel,
    message: string,
    context?: Partial<BaseLogEntry>,
    _error?: unknown
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
        environment: this.isProduction() ? 'production' : 'development'
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
    const authSessionId = this.sessionIdSignal?.();
    if (authSessionId) return authSessionId;
    
    return this.anonymousSessionId!;
  }
  
  private getUserId(): string | null {
    if (this.userIdSignal) {
      return this.userIdSignal();
    }
    return null;
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
  
  private addToBuffer(entry: BaseLogEntry): void {
    this.logBuffer.update(buffer => {
      const newBuffer = [...buffer, entry];
      
      if (newBuffer.length >= this.config.remote.batchSize) {
        this.flush();
        return [];
      }
      
      if (newBuffer.length > this.config.remote.maxBufferSize) {
        return newBuffer.slice(-this.config.remote.maxBufferSize);
      }
      
      return newBuffer;
    });
  }
  
  private flush(): void {
    if (!this.config.remote.enabled) {
      return;
    }
    
    let buffer = this.logBuffer();
    if (buffer.length === 0) {
      return;
    }
    
    if (this.config.remote.filter) {
      buffer = buffer.filter(this.config.remote.filter);
    }
    
    if (buffer.length === 0) {
      this.logBuffer.set([]);
      return;
    }
    
    this.logBuffer.set([]);
    
    this.http.post(this.config.remote.endpoint, { logs: buffer }, { withCredentials: true })
      .subscribe({
        error: (error) => console.error('[Logger] Failed to send logs:', error)
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
      let buffer = this.logBuffer();
      
      if (this.config.remote.filter) {
        buffer = buffer.filter(this.config.remote.filter);
      }
      
      if (buffer.length > 0 && navigator.sendBeacon) {
        navigator.sendBeacon(
          this.config.remote.endpoint,
          JSON.stringify({ logs: buffer })
        );
      }
    });
  }
  
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
  
  private isProduction(): boolean {
    return typeof window !== 'undefined' && 
           (window as any).PRODUCTION === true;
  }
  
  ngOnDestroy(): void {
    if (this.flushTimer) {
      clearInterval(this.flushTimer);
    }
    this.flush();
  }
}

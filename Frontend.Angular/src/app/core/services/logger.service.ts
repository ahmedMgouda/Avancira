import { computed, Injectable, OnDestroy,signal } from '@angular/core';

import { environment } from '../../environments/environment';

export enum LogLevel {
  Trace = -1,
  Debug = 0,
  Info = 1,
  Warn = 2,
  Error = 3,
  Fatal = 4,
  None = 5,
}

export interface LogContext {
  duration?: number;
  status?: number;
  url?: string;
  method?: string;
  headers?: Record<string, string>;
  body?: any;
  response?: any;
  browser?: string;
  platform?: string;
  error?: any;
  memoryUsage?: Record<string, number>;
  performance?: Record<string, number>;
  [key: string]: unknown;
}

export interface LogEntry {
  readonly id: string;
  readonly level: LogLevel;
  readonly message: string;
  readonly timestamp: string;
  readonly context?: LogContext;
  readonly correlationId?: string;
  readonly url?: string;
}

/**
 * Logger Service
 * ─────────────────────────────────────────────────────────────
 * Structured logging with signal-based state
 * Cross-tab synchronization via BroadcastChannel
 * Context enrichment and correlation tracking
 * 
 * ENHANCEMENT: Now implements OnDestroy for proper cleanup
 */
@Injectable({ providedIn: 'root' })
export class LoggerService implements OnDestroy {
  private readonly _maxBufferSize = 1000;
  private _logIdCounter = 0;
  private _correlationId: string | null = null;

  // Signals
  private readonly _logBuffer = signal<LogEntry[]>([]);
  readonly logs = computed(() => this._logBuffer());

  private readonly _logLevel = signal<LogLevel>(
    environment.production
      ? LogLevel.Warn
      : environment.logPolicy?.logLevel ?? LogLevel.Debug
  );

  // Stats
  private _requestCount = 0;
  private _errorCount = 0;
  private _slowCount = 0;

  readonly stats = signal({
    totalRequests: 0,
    totalErrors: 0,
    slowRequests: 0,
    errorRate: '0%',
  });

  // Cross-tab sync
  // NEW: Store reference for cleanup
  private readonly channel = new BroadcastChannel('avancira-logger');

  constructor() {
    console.groupCollapsed('%c🟣 LoggerService', 'color: purple; font-weight: bold');
    console.log(`%cBuffer size: ${this._maxBufferSize}`, 'color: purple');
    console.log(`%cLog level: ${this.getLabel(this._logLevel())}`, 'color: purple');
    console.groupEnd();

    this.channel.onmessage = (e) => {
      const data = e.data;
      if (data?.type === 'new-log') this.addToBuffer(data.entry, false);
      else if (data?.type === 'clear-logs') this.resetLogs(false);
    };
  }

  // ─────────────────────────────────────────────────────────
  // NEW: Lifecycle Management
  // ─────────────────────────────────────────────────────────

  /**
   * Cleanup on service destruction
   * 
   * ENHANCEMENT: Prevents memory leaks from BroadcastChannel
   * 
   * NOTE: Since this is a root-level service, ngOnDestroy
   * only fires when the app is destroyed (page unload).
   * However, it's still good practice for testing and
   * potential future refactoring.
   */
  ngOnDestroy(): void {
    try {
      this.channel.close();
      console.log('%c🟣 LoggerService destroyed, channel closed', 'color: purple');
    } catch (error) {
      console.warn('Failed to close BroadcastChannel:', error);
    }
  }

  // ─────────────────────────────────────────────────────────
  // Public API
  // ─────────────────────────────────────────────────────────

  setLogLevel(level: LogLevel): void {
    this._logLevel.set(level);
    this.info(`Log level changed → ${this.getLabel(level)}`);
  }

  getLogLevel(): LogLevel {
    return this._logLevel();
  }

  setCorrelationId(id: string): void {
    this._correlationId = id;
  }

  getCorrelationId(): string | null {
    return this._correlationId;
  }

  clearCorrelationId(): void {
    this._correlationId = null;
  }

  // Shortcuts
  trace(msg: string, ctx?: LogContext): void {
    this.log(LogLevel.Trace, msg, ctx);
  }

  debug(msg: string, ctx?: LogContext): void {
    this.log(LogLevel.Debug, msg, ctx);
  }

  info(msg: string, ctx?: LogContext): void {
    this.log(LogLevel.Info, msg, ctx);
    this._requestCount++;
    this.updateStats();
  }

  warn(msg: string, ctx?: LogContext): void {
    this.log(LogLevel.Warn, msg, ctx);
    const dur = this.extractDuration(ctx);
    if (dur > 2000) this._slowCount++;
    this.updateStats();
  }

  error(msg: string, err?: unknown, ctx?: LogContext): void {
    this.log(LogLevel.Error, msg, { ...ctx, error: this.serializeError(err) });
    this._errorCount++;
    this.updateStats();
  }

  fatal(msg: string, err?: unknown, ctx?: LogContext): void {
    this.log(LogLevel.Fatal, msg, { ...ctx, error: this.serializeError(err) });
    this._errorCount++;
    this.updateStats();
  }

  clearLogs(): void {
    this.resetLogs(true);
  }

  // ─────────────────────────────────────────────────────────
  // Core Logging
  // ─────────────────────────────────────────────────────────

  private log(level: LogLevel, message: string, context?: LogContext): void {
    if (!this.shouldLog(level)) return;

    const enrichedCtx = this.enrichContext(context);
    const entry: LogEntry = {
      id: `log_${++this._logIdCounter}_${Date.now()}`,
      level,
      message,
      timestamp: new Date().toISOString(),
      context: enrichedCtx,
      correlationId: this._correlationId ?? undefined,
      url: this.safeWindowUrl(),
    };

    this.addToBuffer(entry);
    this.printToConsole(level, message, enrichedCtx);
  }

  private shouldLog(level: LogLevel): boolean {
    const min = this._logLevel();
    return min !== LogLevel.None && level >= min;
  }

  private addToBuffer(entry: LogEntry, broadcast = true): void {
    this._logBuffer.update((buf) => [...buf, entry].slice(-this._maxBufferSize));
    if (broadcast) {
      try {
        this.channel.postMessage({ type: 'new-log', entry });
      } catch (error) {
        // Silently fail if channel is closed (during destroy)
        console.debug('Failed to broadcast log entry:', error);
      }
    }
  }

  private resetLogs(broadcast = true): void {
    this._logBuffer.set([]);
    this._requestCount = this._errorCount = this._slowCount = 0;
    this.updateStats();
    if (broadcast) {
      try {
        this.channel.postMessage({ type: 'clear-logs' });
      } catch (error) {
        console.debug('Failed to broadcast clear-logs:', error);
      }
    }
  }

  // ─────────────────────────────────────────────────────────
  // Context Enrichment
  // ─────────────────────────────────────────────────────────

  private enrichContext(ctx: LogContext = {}): LogContext {
    const enriched: LogContext = { ...ctx };

    // Browser info
    if (typeof navigator !== 'undefined') {
      enriched.browser = navigator.userAgent;
      enriched.platform = navigator.platform;
    }

    // Page URL
    if (!enriched.url) enriched.url = this.safeWindowUrl();

    // Performance metrics (when available)
    if (typeof performance !== 'undefined') {
      const mem = (performance as any).memory;
      enriched.performance = {
        jsHeapUsedSize: mem?.usedJSHeapSize ?? 0,
        jsHeapTotalSize: mem?.totalJSHeapSize ?? 0,
      };
    }

    // Add timing
    if (typeof performance !== 'undefined' && !enriched.duration) {
      const timing = performance.now?.();
      if (timing) enriched.duration = Math.round(timing);
    }

    return enriched;
  }

  // ─────────────────────────────────────────────────────────
  // Console Formatting
  // ─────────────────────────────────────────────────────────

  private printToConsole(level: LogLevel, message: string, context?: LogContext): void {
    const label = this.getLabel(level);
    const color = this.getStyle(level);
    const ts = new Date().toLocaleTimeString();
    const prefix = `${label} [${ts}]`;
    const shortId = this._correlationId ? `[${this._correlationId.slice(0, 8)}]` : '';

    if (context) {
      console.log(`%c${prefix}%c ${shortId} ${message}`, color, 'color:inherit;', context);
    } else {
      console.log(`%c${prefix}%c ${shortId} ${message}`, color, 'color:inherit;');
    }
  }

  private getLabel(level: LogLevel): string {
    return {
      [LogLevel.Trace]: '[TRACE]',
      [LogLevel.Debug]: '[DEBUG]',
      [LogLevel.Info]: '[INFO]',
      [LogLevel.Warn]: '[WARN]',
      [LogLevel.Error]: '[ERROR]',
      [LogLevel.Fatal]: '[FATAL]',
      [LogLevel.None]: '[NONE]',
    }[level] ?? '[LOG]';
  }

  private getStyle(level: LogLevel): string {
    return {
      [LogLevel.Trace]: 'color: gray;',
      [LogLevel.Debug]: 'color: #1e90ff;',
      [LogLevel.Info]: 'color: #10b981;',
      [LogLevel.Warn]: 'color: #f59e0b; font-weight:bold;',
      [LogLevel.Error]: 'color: #ef4444; font-weight:bold;',
      [LogLevel.Fatal]: 'color: #7e22ce; font-weight:bold;',
      [LogLevel.None]: 'color: inherit;',
    }[level] ?? 'color: inherit;';
  }

  // ─────────────────────────────────────────────────────────
  // Utilities
  // ─────────────────────────────────────────────────────────

  private serializeError(err: unknown): any {
    if (!err) return null;
    return err instanceof Error
      ? { name: err.name, message: err.message, stack: err.stack }
      : err;
  }

  private extractDuration(ctx?: LogContext): number {
    const val = String(ctx?.duration ?? '0');
    return parseInt(val.replace(/\D/g, ''), 10);
  }

  private safeWindowUrl(): string {
    try {
      return typeof window !== 'undefined' ? window.location.href : '';
    } catch {
      return '';
    }
  }

  private updateStats(): void {
    const errorRate = this._requestCount
      ? ((this._errorCount / this._requestCount) * 100).toFixed(2) + '%'
      : '0%';
    this.stats.set({
      totalRequests: this._requestCount,
      totalErrors: this._errorCount,
      slowRequests: this._slowCount,
      errorRate,
    });
  }
}
import { BrowserCompat } from './browser-compat.util';

/**
 * ID Generator Utility
 * ═══════════════════════════════════════════════════════════════════════
 * Generates consistent IDs for logs, traces, spans, sessions, etc.
 */
export class IdGenerator {
  private static logCounter = 0;
  private static errorCounter = 0;

  /** Generate unique log ID */
  static generateLogId(): string {
    return `log-${Date.now()}-${++this.logCounter}`;
  }

  /** Generate unique error ID */
  static generateErrorId(): string {
    return `err-${Date.now()}-${++this.errorCounter}`;
  }

  /** Generate W3C-compliant 16-hex span ID */
  static generateSpanId(): string {
    const uuid = BrowserCompat.generateUUID().replace(/-/g, '');
    // Span IDs must be 16 lowercase hex chars
    return uuid.substring(0, 16).toLowerCase();
  }

  /** Generate W3C-compliant 32-hex trace ID */
  static generateTraceId(): string {
    const uuid = BrowserCompat.generateUUID().replace(/-/g, '');
    // Trace IDs must be 32 lowercase hex chars
    return uuid.substring(0, 32).toLowerCase();
  }

  /** Generate session ID (not W3C-specific) */
  static generateSessionId(): string {
    return `sess-${BrowserCompat.generateUUID()}`;
  }
}

/**
 * Consolidated ID Generator
 * ═══════════════════════════════════════════════════════════════════════
 * Single source of truth for all ID generation across the application
 * Replaces: IdGenerator (logging) + BrowserCompat UUID generation
 * 
 * Features:
 *   ✅ Browser-compatible UUID generation with fallback
 *   ✅ W3C Trace Context compliant IDs (16/32 hex chars)
 *   ✅ Domain-specific ID prefixes for debugging
 *   ✅ Performance optimized (uses native crypto when available)
 */

export class IdGenerator {
  private static logCounter = 0;
  private static errorCounter = 0;
  
  // Cache native support check
  private static _hasNativeUUID: boolean | null = null;

  /**
   * Check if crypto.randomUUID is supported
   */
  private static get hasNativeUUID(): boolean {
    if (this._hasNativeUUID === null) {
      this._hasNativeUUID =
        typeof crypto !== 'undefined' && 
        typeof crypto.randomUUID === 'function';
    }
    return this._hasNativeUUID;
  }

  /**
   * Generate RFC4122 v4 compliant UUID
   * Uses native crypto.randomUUID() when available, otherwise fallback
   * 
   * @returns UUID string (e.g., "550e8400-e29b-41d4-a716-446655440000")
   */
  static generateUUID(): string {
    if (this.hasNativeUUID) {
      return crypto.randomUUID();
    }

    // Fallback implementation (RFC4122 v4 compliant)
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
      const r = (Math.random() * 16) | 0;
      const v = c === 'x' ? r : (r & 0x3) | 0x8;
      return v.toString(16);
    });
  }

  /**
   * Generate unique log ID with counter for ordering
   * Format: log-{timestamp}-{counter}
   */
  static generateLogId(): string {
    return `log-${Date.now()}-${++this.logCounter}`;
  }

  /**
   * Generate unique error ID with counter for ordering
   * Format: err-{timestamp}-{counter}
   */
  static generateErrorId(): string {
    return `err-${Date.now()}-${++this.errorCounter}`;
  }

  /**
   * Generate W3C-compliant 16-character hex span ID
   * Used in distributed tracing (OpenTelemetry, Jaeger, Zipkin)
   * 
   * @returns 16 lowercase hex characters
   */
  static generateSpanId(): string {
    const uuid = this.generateUUID().replace(/-/g, '');
    return uuid.substring(0, 16).toLowerCase();
  }

  /**
   * Generate W3C-compliant 32-character hex trace ID
   * Used in distributed tracing (OpenTelemetry, Jaeger, Zipkin)
   * 
   * @returns 32 lowercase hex characters
   */
  static generateTraceId(): string {
    const uuid = this.generateUUID().replace(/-/g, '');
    return uuid.substring(0, 32).toLowerCase();
  }

  /**
   * Generate session ID (not W3C-specific)
   * Format: sess-{uuid}
   */
  static generateSessionId(): string {
    return `sess-${this.generateUUID()}`;
  }

  /**
   * Generate toast notification ID
   * Format: toast-{timestamp}-{random}
   */
  static generateToastId(): string {
    return `toast-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
  }

  /**
   * Generate request tracking ID
   * Format: req-{uuid}
   */
  static generateRequestId(): string {
    return `req-${this.generateUUID()}`;
  }

  /**
   * Get browser UUID support info (for diagnostics)
   */
  static getDiagnostics() {
    return {
      hasNativeUUID: this.hasNativeUUID,
      userAgent: navigator.userAgent,
      platform: navigator.platform,
      logCounter: this.logCounter,
      errorCounter: this.errorCounter
    };
  }
}
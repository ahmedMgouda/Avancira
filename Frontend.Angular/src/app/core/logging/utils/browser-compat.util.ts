/**
 * Browser compatibility utilities for logging system
 * Provides fallbacks for modern APIs that may not be available in all browsers
 */
export class BrowserCompat {
  private static _uuidSupport: boolean | null = null;
  private static _sendBeaconSupport: boolean | null = null;
  private static _sessionStorageSupport: boolean | null = null;

  /**
   * Generate UUID with fallback for browsers without crypto.randomUUID()
   */
  static generateUUID(): string {
    // Check for native support
    if (typeof crypto !== 'undefined' && crypto.randomUUID) {
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
   * Check if crypto.randomUUID is supported
   */
  static hasUUIDSupport(): boolean {
    if (this._uuidSupport === null) {
      this._uuidSupport =
        typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function';
    }
    return this._uuidSupport;
  }

  /**
   * Check if navigator.sendBeacon is supported
   */
  static hasSendBeaconSupport(): boolean {
    if (this._sendBeaconSupport === null) {
      this._sendBeaconSupport =
        typeof navigator !== 'undefined' && typeof navigator.sendBeacon === 'function';
    }
    return this._sendBeaconSupport;
  }

  /**
   * Check if sessionStorage is available
   */
  static hasSessionStorageSupport(): boolean {
    if (this._sessionStorageSupport === null) {
      try {
        const test = '__storage_test__';
        sessionStorage.setItem(test, test);
        sessionStorage.removeItem(test);
        this._sessionStorageSupport = true;
      } catch {
        this._sessionStorageSupport = false;
      }
    }
    return this._sessionStorageSupport;
  }

  /**
   * Safe sessionStorage.getItem with fallback
   */
  static getSessionItem(key: string): string | null {
    if (!this.hasSessionStorageSupport()) {
      return null;
    }
    try {
      return sessionStorage.getItem(key);
    } catch {
      return null;
    }
  }

  /**
   * Safe sessionStorage.setItem with fallback
   */
  static setSessionItem(key: string, value: string): boolean {
    if (!this.hasSessionStorageSupport()) {
      return false;
    }
    try {
      sessionStorage.setItem(key, value);
      return true;
    } catch {
      return false;
    }
  }

  /**
   * Get browser and platform info
   */
  static getBrowserInfo(): {
    userAgent: string;
    platform: string;
    language: string;
    hasUUIDSupport: boolean;
    hasBeaconSupport: boolean;
    hasSessionStorage: boolean;
  } {
    return {
      userAgent: navigator.userAgent,
      platform: navigator.platform,
      language: navigator.language,
      hasUUIDSupport: this.hasUUIDSupport(),
      hasBeaconSupport: this.hasSendBeaconSupport(),
      hasSessionStorage: this.hasSessionStorageSupport()
    };
  }
}
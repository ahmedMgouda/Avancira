import { BrowserCompat } from './browser-compat.util';

export class IdGenerator {
  private static logCounter = 0;
  private static errorCounter = 0;
  private static spanCounter = 0;

  static generateLogId(): string {
    return `log-${Date.now()}-${++this.logCounter}`;
  }

  static generateErrorId(): string {
    return `err-${Date.now()}-${++this.errorCounter}`;
  }

  static generateSpanId(): string {
    return `span-${Date.now()}-${++this.spanCounter}`;
  }

  static generateTraceId(): string {
    return `trace-${BrowserCompat.generateUUID()}`;
  }

  static generateSessionId(): string {
    return `sess-${BrowserCompat.generateUUID()}`;
  }
}
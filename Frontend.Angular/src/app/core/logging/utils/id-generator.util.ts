export class IdGenerator {
  static generateLogId(): string {
    return `log-${this.generateId()}`;
  }
  
  static generateErrorId(): string {
    return `err-${this.generateId()}`;
  }
  
  static generateTraceId(): string {
    return `trace-${this.generateId()}`;
  }
  
  static generateSpanId(): string {
    return `span-${this.generateId()}`;
  }
  
  private static generateId(): string {
    try {
      if (typeof crypto !== 'undefined' && crypto.randomUUID) {
        return crypto.randomUUID();
      }
    } catch {
    }
    
    return `${Date.now()}-${Math.random().toString(36).substring(2, 11)}`;
  }
}

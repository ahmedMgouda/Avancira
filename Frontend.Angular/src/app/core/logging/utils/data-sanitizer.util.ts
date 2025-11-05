export class DataSanitizer {
  private readonly sensitiveFields: string[];
  private readonly redactedValue: string;
  
  constructor(sensitiveFields: string[], redactedValue: string) {
    this.sensitiveFields = sensitiveFields.map(f => f.toLowerCase());
    this.redactedValue = redactedValue;
  }
  
  sanitize(data: unknown): unknown {
    if (!data || typeof data !== 'object') {
      return data;
    }
    
    if (Array.isArray(data)) {
      return data.map(item => this.sanitize(item));
    }
    
    const sanitized: Record<string, unknown> = {};
    
    for (const [key, value] of Object.entries(data)) {
      const lowerKey = key.toLowerCase();
      
      if (this.isSensitiveField(lowerKey)) {
        sanitized[key] = this.redactedValue;
      } else if (typeof value === 'object' && value !== null) {
        sanitized[key] = this.sanitize(value);
      } else {
        sanitized[key] = value;
      }
    }
    
    return sanitized;
  }
  
  private isSensitiveField(fieldName: string): boolean {
    return this.sensitiveFields.some(sf => fieldName.includes(sf));
  }
}

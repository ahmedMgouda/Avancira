import { BaseLogEntry } from '../models/base-log-entry.model';
import { LogLevel } from '../models/log-level.model';

export interface LoggingConfig {
  enabled: boolean;
  minLevel: LogLevel;
  
  application: {
    name: string;
    version: string;
  };
  
  console: {
    enabled: boolean;
    useColors: boolean;
  };
  
  remote: {
    enabled: boolean;
    endpoint: string;
    batchSize: number;
    flushInterval: number;
    maxBufferSize: number;
    filter?: (log: BaseLogEntry) => boolean;
  };
  
  http: {
    enabled: boolean;
    logRequestBodies: boolean;
    logResponseBodies: boolean;
    slowRequestThreshold: number;
  };
  
  sanitization: {
    enabled: boolean;
    sensitiveFields: string[];
    redactedValue: string;
  };
}

export function getLoggingConfig(isProduction: boolean): LoggingConfig {
  return {
    enabled: true,
    minLevel: isProduction ? LogLevel.INFO : LogLevel.DEBUG,
    
    application: {
      name: 'avancira-frontend',
      version: '1.0.0'
    },
    
    console: {
      enabled: !isProduction,
      useColors: true
    },
    
    remote: {
      enabled: true,
      endpoint: '/api/logs',
      batchSize: 10,
      flushInterval: 5000,
      maxBufferSize: 100
    },
    
    http: {
      enabled: true,
      logRequestBodies: false,
      logResponseBodies: false,
      slowRequestThreshold: 3000
    },
    
    sanitization: {
      enabled: true,
      sensitiveFields: [
        'password',
        'token',
        'authorization',
        'api_key',
        'secret',
        'credit_card',
        'ssn'
      ],
      redactedValue: '[REDACTED]'
    }
  };
}

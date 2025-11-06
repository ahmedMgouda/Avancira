import { environment } from '../../../environments/environment';
import { LogLevel } from '../models/log-level.model';

export interface UserTypeLoggingConfig {
  enabled: boolean;
  minLevel: LogLevel;
  samplingRate: number; // 0.0 to 1.0 (0% to 100%)
  includeUserData: boolean;
  logActions: string[]; // ['*'] for all, or specific actions
  maxBufferSize: number;
}

export interface LoggingConfig {
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
    retry: {
      enabled: boolean;
      maxRetries: number;
      baseDelayMs: number;
      maxDelayMs: number;
    };
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
  
  /** Configuration per user type */
  authenticated: UserTypeLoggingConfig;
  anonymous: UserTypeLoggingConfig;
  
  /** Buffer overflow warning threshold (% of maxBufferSize) */
  bufferWarningThreshold: number;
  
  /** Deduplication settings */
  deduplication: {
    enabled: boolean;
    timeWindowMs: number; // Time window to check for duplicates
    maxCacheSize: number; // Max number of recent log hashes to keep
  };
}

export function getLoggingConfig(): LoggingConfig {
  const isProduction = environment.production;
  
  return {
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
      retry: {
        enabled: true,
        maxRetries: 3,
        baseDelayMs: 1000,
        maxDelayMs: 30000
      }
    },
    
    http: {
      enabled: true,
      logRequestBodies: false,
      logResponseBodies: false,
      slowRequestThreshold: environment.logPolicy?.slowThresholdMs ?? 3000
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
        'ssn',
        'accessToken',
        'refreshToken',
        'apiKey',
        'privateKey',
        'Bearer'
      ],
      redactedValue: '[REDACTED]'
    },
    
    // Authenticated users: Full logging
    authenticated: {
      enabled: true,
      minLevel: isProduction ? LogLevel.INFO : LogLevel.DEBUG,
      samplingRate: 1.0, // 100% - log everything
      includeUserData: true,
      logActions: ['*'], // All actions
      maxBufferSize: 100
    },
    
    // Anonymous users: Minimal logging
    anonymous: {
      enabled: true,
      minLevel: isProduction ? LogLevel.ERROR : LogLevel.WARN,
      samplingRate: isProduction ? 0.05 : 1.0, // 5% in prod, 100% in dev
      includeUserData: false, // Never include user data for anonymous
      logActions: ['error', 'fatal', 'http_error'], // Only critical
      maxBufferSize: 20 // Smaller buffer
    },
    
    bufferWarningThreshold: 0.8, // Warn at 80% capacity
    
    deduplication: {
      enabled: true,
      timeWindowMs: 5000, // 5 seconds
      maxCacheSize: 100
    }
  };
}
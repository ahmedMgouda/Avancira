/**
 * Base Log Entry Structure
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * IMPROVED:
 * ✅ Supports custom fields via index signature
 * ✅ All standard fields are optional for flexibility
 * ✅ Type-safe but extensible
 * 
 * USAGE:
 * logger.info('User action', {
 *   customField: 'value',
 *   business: { orderId: 123 },
 *   http: { ... }
 * });
 */

export interface BaseLogEntry {
  '@timestamp': string;
  '@version': string;
  
  log: {
    id?: string;
    level?: string;
    source?: string;
    type?: string;
    message?: string;
  };
  
  service: {
    name: string;
    version: string;
    environment: string;
  };
  
  trace: {
    id: string;
    span?: {
      id: string;
      name: string;
      duration_ms?: number;
    };
  };
  
  client: {
    url: string;
    route: string;
    user_agent: string;
  };
  
  session?: {
    id: string;
    user?: {
      id: string;
    };
  };
  
  http?: {
    method: string;
    url: string;
    status_code?: number;
    duration_ms?: number;
    problem_details?: {
      type: string;
      title: string;
      status: number;
      detail: string;
      instance?: string;
    };
    error_message?: string;
  };
  
  error?: {
    id: string;
    kind: 'application' | 'system';
    handled: boolean;
    code: string;
    type: string;
    message: {
      user: string;
      technical: string;
    };
    severity: 'info' | 'warning' | 'error' | 'critical';
    stack?: string;
    source?: {
      component?: string;
      file?: string;
      line?: number;
      method?: string;
    };
  };
  
  navigation?: {
    from: string;
    to: string;
    trigger: 'user' | 'code' | 'browser';
  };

  // ✅ ALLOW CUSTOM FIELDS
  // This enables users to add any custom data to logs
  [key: string]: any;
}

/**
 * Helper type for creating logs with custom data
 * Use this when you want to add custom fields to your logs
 */
export type LogContext = Partial<BaseLogEntry> & {
  // Custom data can be added here
  [key: string]: any;
};

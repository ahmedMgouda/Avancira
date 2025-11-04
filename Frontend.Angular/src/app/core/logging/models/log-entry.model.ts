/**
 * Unified Logging Models
 * Based on OpenTelemetry standards for distributed tracing
 * 
 * Hierarchy:
 * - BaseLogEntry: Common fields for all logs
 * - Specialized entries: HttpLogEntry, NavigationLogEntry, etc.
 */

/**
 * Log severity levels
 * Aligned with standard logging frameworks
 */
export enum LogLevel {
  TRACE = 0,
  DEBUG = 1,
  INFO = 2,
  WARN = 3,
  ERROR = 4,
  FATAL = 5
}

/**
 * Log categories for filtering and analysis
 */
export type LogCategory =
  | 'navigation'    // Route changes, trace boundaries
  | 'http'          // API calls
  | 'ui'            // User interactions, component events
  | 'error'         // Errors and exceptions
  | 'business'      // Business logic events
  | 'performance'   // Performance metrics
  | 'security';     // Auth, permission events

/**
 * Base log entry - common fields for all logs
 * 
 * DESIGN NOTES:
 * - `source` is always 'frontend' for this application
 * - `traceId` groups all logs within a navigation session
 * - `spanId` identifies a specific operation or interaction
 * - `parentSpanId` creates the parent-child relationship
 */
export interface BaseLogEntry {
  // ─────────────────────────────────────────────────────────
  // Timestamp
  // ─────────────────────────────────────────────────────────
  
  /** ISO 8601 timestamp */
  timestamp: string;
  
  // ─────────────────────────────────────────────────────────
  // Distributed Tracing (OpenTelemetry-inspired)
  // ─────────────────────────────────────────────────────────
  
  /** Unique identifier for the entire user journey (navigation session) */
  traceId: string;
  
  /** Unique identifier for this specific operation */
  spanId: string;
  
  /** Links child operations to parent (null for root spans) */
  parentSpanId: string | null;
  
  /** Duration of the span in milliseconds (null if still active) */
  spanDuration: number | null;
  
  // ─────────────────────────────────────────────────────────
  // Session & User Context
  // ─────────────────────────────────────────────────────────
  
  /** Backend session ID (from /bff/user endpoint) - null for unauthenticated */
  sessionId: string | null;
  
  /** Authenticated user ID - null for anonymous users */
  userId: string | null;
  
  // ─────────────────────────────────────────────────────────
  // Classification
  // ─────────────────────────────────────────────────────────
  
  /** Log severity level */
  level: LogLevel;
  
  /** Always 'frontend' for this application */
  source: 'frontend';
  
  /** Type/category of log for filtering */
  category: LogCategory;
  
  // ─────────────────────────────────────────────────────────
  // Content
  // ─────────────────────────────────────────────────────────
  
  /** Human-readable log message */
  message: string;
  
  // ─────────────────────────────────────────────────────────
  // Application Metadata
  // ─────────────────────────────────────────────────────────
  
  /** Runtime environment */
  environment: 'production' | 'development';
  
  /** Application name (for multi-app logging systems) */
  application: string;
  
  /** Application version (from package.json) */
  version: string;
  
  /** Browser/client information */
  userAgent?: string;
  
  /** Current route/URL */
  url?: string;
}

// ═════════════════════════════════════════════════════════════
// Specialized Log Entry Types
// ═════════════════════════════════════════════════════════════

/**
 * Navigation/Route Change Log Entry
 * Created when user navigates to new route (creates new trace)
 */
export interface NavigationLogEntry extends BaseLogEntry {
  category: 'navigation';
  
  navigation: {
    /** Previous route */
    fromRoute: string;
    
    /** New route */
    toRoute: string;
    
    /** Navigation trigger ('user' | 'code' | 'browser') */
    trigger: 'user' | 'code' | 'browser';
    
    /** Query parameters */
    queryParams?: Record<string, string>;
  };
}

/**
 * HTTP Request/Response Log Entry
 * Created for each API call
 */
export interface HttpLogEntry extends BaseLogEntry {
  category: 'http';
  
  http: {
    /** HTTP method */
    method: string;
    
    /** Full URL with query params */
    url: string;
    
    /** HTTP status code (null for ongoing requests) */
    statusCode: number | null;
    
    /** Request duration in ms (null for ongoing requests) */
    duration: number | null;
    
    /** Request headers (sanitized) */
    requestHeaders?: Record<string, string>;
    
    /** Response headers (sanitized) */
    responseHeaders?: Record<string, string>;
    
    /** Request body (sanitized) */
    requestBody?: unknown;
    
    /** Response body (sanitized) */
    responseBody?: unknown;
    
    /** Error details if request failed */
    error?: HttpErrorDetails;
  };
}

export interface HttpErrorDetails {
  /** Error type */
  type: 'network' | 'timeout' | 'server' | 'client' | 'unknown';
  
  /** Error message */
  message: string;
  
  /** Error code if available */
  code?: string;
}

/**
 * User Interaction Log Entry
 * Created when user performs an action (click, submit, etc.)
 */
export interface UserInteractionLogEntry extends BaseLogEntry {
  category: 'ui';
  
  interaction: {
    /** Type of interaction */
    type: 'click' | 'submit' | 'input' | 'scroll' | 'custom';
    
    /** Target element (button id, form name, etc.) */
    target: string;
    
    /** Component name if available */
    component?: string;
    
    /** Additional context */
    context?: Record<string, unknown>;
  };
}

/**
 * Error Log Entry
 * Created when an error/exception occurs
 */
export interface ErrorLogEntry extends BaseLogEntry {
  category: 'error';
  level: LogLevel.ERROR | LogLevel.FATAL;
  
  error: {
    /** Error type/name */
    type: string;
    
    /** Error message */
    message: string;
    
    /** Stack trace (only in development) */
    stack?: string;
    
    /** Component/service where error occurred */
    source?: string;
    
    /** Additional error context */
    context?: Record<string, unknown>;
  };
}

/**
 * Business Logic Log Entry
 * Created for important business operations
 */
export interface BusinessLogEntry extends BaseLogEntry {
  category: 'business';
  
  business: {
    /** Operation name (e.g., 'create_order', 'checkout') */
    operation: string;
    
    /** Entity type (e.g., 'Order', 'User', 'Payment') */
    entity?: string;
    
    /** Entity ID if applicable */
    entityId?: string;
    
    /** Operation result */
    result: 'success' | 'failure' | 'partial';
    
    /** Additional business context */
    context?: Record<string, unknown>;
  };
}

/**
 * Performance Log Entry
 * Created for performance monitoring
 */
export interface PerformanceLogEntry extends BaseLogEntry {
  category: 'performance';
  
  performance: {
    /** Metric type */
    metric: 'page_load' | 'component_render' | 'api_call' | 'custom';
    
    /** Metric name */
    name: string;
    
    /** Duration in milliseconds */
    duration: number;
    
    /** Performance marks if available */
    marks?: Record<string, number>;
    
    /** Additional metrics */
    metrics?: Record<string, number>;
  };
}

/**
 * Security Log Entry
 * Created for security-related events
 */
export interface SecurityLogEntry extends BaseLogEntry {
  category: 'security';
  
  security: {
    /** Security event type */
    event: 'login' | 'logout' | 'unauthorized' | 'forbidden' | 'csrf' | 'xss' | 'custom';
    
    /** Event result */
    result: 'success' | 'failure' | 'blocked';
    
    /** Additional security context */
    context?: Record<string, unknown>;
  };
}

/**
 * Union type of all log entry types
 * Used for type-safe log handling
 */
export type UnifiedLogEntry =
  | NavigationLogEntry
  | HttpLogEntry
  | UserInteractionLogEntry
  | ErrorLogEntry
  | BusinessLogEntry
  | PerformanceLogEntry
  | SecurityLogEntry
  | BaseLogEntry; // Fallback for generic logs

/**
 * Log context for additional metadata
 * Used in logger methods to pass extra information
 */
export interface LogContext {
  [key: string]: unknown;
}

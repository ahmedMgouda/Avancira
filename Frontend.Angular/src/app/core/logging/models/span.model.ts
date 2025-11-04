/**
 * Span Model
 * Represents a single operation or interaction within a trace
 * 
 * TYPES:
 * - Root Span: User action (parentSpanId = null)
 * - Child Span: API call, sub-operation (parentSpanId = parent root span)
 * - Background Span: Async operation (parentSpanId = null or current root)
 * 
 * LIFECYCLE:
 * - Created: On operation start
 * - Active: During operation
 * - Ended: On operation complete, error, or timeout
 */

/**
 * Span status
 */
export enum SpanStatus {
  ACTIVE = 'active',
  ENDED = 'ended',
  ERROR = 'error'
}

/**
 * Span kind (inspired by OpenTelemetry)
 */
export enum SpanKind {
  /** Root span - user interaction */
  ROOT = 'root',
  
  /** Child span - sub-operation */
  CHILD = 'child',
  
  /** Background span - async operation */
  BACKGROUND = 'background'
}

/**
 * Span interface
 * Represents a single operation within a trace
 */
export interface Span {
  /** Unique span identifier */
  spanId: string;
  
  /** Parent trace ID */
  traceId: string;
  
  /** Parent span ID (null for root spans) */
  parentSpanId: string | null;
  
  /** Span kind */
  kind: SpanKind;
  
  /** Operation name (e.g., 'create-order', 'GET /api/orders') */
  name: string;
  
  /** When span was created */
  startTime: Date;
  
  /** When span ended (null if still active) */
  endTime: Date | null;
  
  /** Span duration in milliseconds (null if still active) */
  duration: number | null;
  
  /** Current span status */
  status: SpanStatus;
  
  /** Additional attributes/context */
  attributes: Record<string, unknown>;
}

/**
 * Root span creation options
 * Used for user interactions
 */
export interface RootSpanOptions {
  /** Span name (e.g., 'create-order') */
  name: string;
  
  /** Additional attributes */
  attributes?: Record<string, unknown>;
}

/**
 * Child span creation options
 * Used for API calls and sub-operations
 */
export interface ChildSpanOptions {
  /** Span name (e.g., 'GET /api/orders') */
  name: string;
  
  /** Parent span ID */
  parentSpanId: string;
  
  /** Additional attributes */
  attributes?: Record<string, unknown>;
}

/**
 * Background span creation options
 * Used for async/background operations
 */
export interface BackgroundSpanOptions {
  /** Span name (e.g., 'auto-save-draft') */
  name: string;
  
  /** Whether to attach to current trace (default: true) */
  attachToCurrentTrace?: boolean;
  
  /** Additional attributes */
  attributes?: Record<string, unknown>;
}

/**
 * Span end options
 */
export interface SpanEndOptions {
  /** Span status */
  status?: SpanStatus;
  
  /** Error if span failed */
  error?: Error;
  
  /** Additional attributes to add on end */
  attributes?: Record<string, unknown>;
}

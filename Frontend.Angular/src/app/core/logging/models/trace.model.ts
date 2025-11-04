/**
 * Trace Model
 * Represents a complete user journey within a single navigation session
 * 
 * LIFECYCLE:
 * - Created: On navigation start
 * - Active: During navigation session
 * - Ended: On next navigation or app close
 */

/**
 * Trace status
 */
export enum TraceStatus {
  ACTIVE = 'active',
  ENDED = 'ended'
}

/**
 * Trace interface
 * Groups all spans within a navigation session
 */
export interface Trace {
  /** Unique trace identifier */
  traceId: string;
  
  /** When trace was created */
  startTime: Date;
  
  /** When trace ended (null if still active) */
  endTime: Date | null;
  
  /** Current trace status */
  status: TraceStatus;
  
  /** Route that started this trace */
  route: string;
  
  /** Previous route (null if first navigation) */
  previousRoute: string | null;
  
  /** Session ID (null if unauthenticated) */
  sessionId: string | null;
  
  /** User ID (null if unauthenticated) */
  userId: string | null;
  
  /** Number of spans in this trace */
  spanCount: number;
  
  /** Navigation trigger */
  trigger: 'user' | 'code' | 'browser';
}

/**
 * Trace creation options
 */
export interface TraceOptions {
  route: string;
  previousRoute: string | null;
  sessionId: string | null;
  userId: string | null;
  trigger?: 'user' | 'code' | 'browser';
}
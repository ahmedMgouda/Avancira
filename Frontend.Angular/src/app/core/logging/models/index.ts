// core/logging/models/index.ts
/**
 * Logging Models - Centralized Exports
 * ═══════════════════════════════════════════════════════════════════════
 * Single source of truth for all logging types
 * 
 * STRUCTURE:
 * - Base log entry (ECS format)
 * - Log levels and types
 * - Standard error model
 * - Trace/Span models (re-exported from service)
 */

// ─────────────────────────────────────────────────────────────────────
// Base Log Entry (ECS Format)
// ─────────────────────────────────────────────────────────────────────
export type { BaseLogEntry } from './base-log-entry.model';

// ─────────────────────────────────────────────────────────────────────
// Log Levels & Types
// ─────────────────────────────────────────────────────────────────────
export type { LogType } from './log-level.model';
export { LogLevel } from './log-level.model';

// ─────────────────────────────────────────────────────────────────────
// Standard Error Model
// ─────────────────────────────────────────────────────────────────────
export type { StandardError } from './standard-error.model';

// ─────────────────────────────────────────────────────────────────────
// Trace/Span Models
// ─────────────────────────────────────────────────────────────────────
// NOTE: These are re-exported from TraceService to avoid circular dependencies
// The canonical definition is in services/trace.service.ts
export type { 
  Span, 
  TraceContext 
} from '../services/trace.service';
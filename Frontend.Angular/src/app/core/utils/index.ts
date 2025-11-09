// core/utils/index.ts
/**
 * Core Utilities - Public API
 * ═══════════════════════════════════════════════════════════════════════
 * Reusable utilities for the entire application
 */

// ─────────────────────────────────────────────────────────────────────
// Buffer Management
// ─────────────────────────────────────────────────────────────────────
export { 
  type BufferConfig,
  type BufferDiagnostics, 
  BufferManager} from './buffer-manager.utility';

// ─────────────────────────────────────────────────────────────────────
// Cleanup Management
// ─────────────────────────────────────────────────────────────────────
export { 
  CleanableService, 
  CleanupManager} from './cleanup-manager.utility';

// ─────────────────────────────────────────────────────────────────────
// Deduplication
// ─────────────────────────────────────────────────────────────────────
export {
  type DedupConfig,
  DedupManager,
  type DedupStats
} from './dedup-manager.utility';

// ─────────────────────────────────────────────────────────────────────
// Entity Caching
// ─────────────────────────────────────────────────────────────────────
export {
  type CacheConfig,
  EntityCache} from './entity-cache.utility';

// ─────────────────────────────────────────────────────────────────────
// Error Classification
// ─────────────────────────────────────────────────────────────────────
export {
  type ErrorCategory,
  type ErrorClassification,
  ErrorClassifier} from './error-classifier.utility';

// ─────────────────────────────────────────────────────────────────────
// ID Generation
// ─────────────────────────────────────────────────────────────────────
export { IdGenerator } from './id-generator.utility';

// ─────────────────────────────────────────────────────────────────────
// Sampling
// ─────────────────────────────────────────────────────────────────────
export {
  type SamplingConfig,
  SamplingManager,
  type SamplingStats
} from './sampling-manager.utility';
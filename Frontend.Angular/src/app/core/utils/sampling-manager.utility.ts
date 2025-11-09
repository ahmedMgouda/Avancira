// core/utils/sampling-manager.utility.ts
/**
 * Type-Based Sampling Manager
 * ═══════════════════════════════════════════════════════════════════════
 * Configurable sampling with per-type rates
 * 
 * Used by:
 * - LoggerService (sample by log type)
 * - Potentially other services needing probabilistic filtering
 * 
 * Features:
 * ✅ Per-type sampling rates (0.0 - 1.0)
 * ✅ Default rate for unknown types
 * ✅ Always-include list (never sample out)
 * ✅ Statistics tracking (acceptance rate per type)
 * ✅ Dynamic configuration updates
 * 
 * @example
 * const sampling = new SamplingManager({
 *   enabled: true,
 *   defaultRate: 0.1, // 10% default
 *   rates: {
 *     'error': 1.0,   // 100% of errors
 *     'debug': 0.05   // 5% of debug logs
 *   },
 *   alwaysInclude: ['error', 'fatal']
 * });
 * 
 * if (sampling.shouldSample('debug')) {
 *   // Process debug log
 * }
 */

export interface SamplingConfig {
  /** Enable/disable sampling globally */
  enabled: boolean;

  /** Default rate for types not specified (0.0 - 1.0) */
  defaultRate: number;

  /** Per-type sampling rates (0.0 = drop all, 1.0 = keep all) */
  rates: Record<string, number>;

  /** Types that should never be sampled out */
  alwaysInclude?: string[];
}

export interface SamplingStats {
  /** Statistics by type */
  byType: Record<string, TypeStats>;

  /** Overall statistics */
  overall: OverallStats;
}

interface TypeStats {
  /** Total items checked */
  checked: number;

  /** Items sampled in (kept) */
  sampled: number;

  /** Actual sampling rate (sampled / checked) */
  rate: number;
}

interface OverallStats {
  /** Total items checked */
  checked: number;

  /** Items sampled in */
  sampled: number;

  /** Overall sampling rate */
  rate: number;
}

export class SamplingManager {
  private config: SamplingConfig;
  private stats = new Map<string, { checked: number; sampled: number }>();
  private overallStats = { checked: 0, sampled: 0 };

  constructor(config: SamplingConfig) {
    this.config = { ...config };
    this.validateConfig();
  }

  /**
   * Check if item should be sampled (kept)
   * @param type Item type
   * @returns true = KEEP, false = DROP
   */
  shouldSample(type: string): boolean {
    // If sampling disabled, keep everything
    if (!this.config.enabled) {
      return true;
    }

    // Check if type should always be included
    if (this.config.alwaysInclude?.includes(type)) {
      return true;
    }

    // Get sampling rate for this type
    const rate = this.config.rates[type] ?? this.config.defaultRate;

    // Sample decision (probabilistic)
    const shouldKeep = Math.random() < rate;

    return shouldKeep;
  }

  /**
   * Record sampling decision for statistics
   * @param type Item type
   * @param sampled Whether item was sampled in
   */
  recordDecision(type: string, sampled: boolean): void {
    // Update type-specific stats
    let typeStats = this.stats.get(type);
    if (!typeStats) {
      typeStats = { checked: 0, sampled: 0 };
      this.stats.set(type, typeStats);
    }

    typeStats.checked++;
    if (sampled) {
      typeStats.sampled++;
    }

    // Update overall stats
    this.overallStats.checked++;
    if (sampled) {
      this.overallStats.sampled++;
    }
  }

  /**
   * Get sampling statistics
   */
  getStats(): SamplingStats {
    const byType: Record<string, TypeStats> = {};

    for (const [type, stats] of this.stats.entries()) {
      byType[type] = {
        checked: stats.checked,
        sampled: stats.sampled,
        rate: stats.checked > 0 ? (stats.sampled / stats.checked) : 0
      };
    }

    return {
      byType,
      overall: {
        checked: this.overallStats.checked,
        sampled: this.overallStats.sampled,
        rate: this.overallStats.checked > 0 
          ? (this.overallStats.sampled / this.overallStats.checked) 
          : 0
      }
    };
  }

  /**
   * Update configuration dynamically
   * @param config Partial config to update
   */
  updateConfig(config: Partial<SamplingConfig>): void {
    this.config = { ...this.config, ...config };
    this.validateConfig();
  }

  /**
   * Get current configuration
   */
  getConfig(): Readonly<SamplingConfig> {
    return { ...this.config };
  }

  /**
   * Reset statistics
   */
  resetStats(): void {
    this.stats.clear();
    this.overallStats = { checked: 0, sampled: 0 };
  }

  /**
   * Get sampling rate for a specific type
   */
  getRateForType(type: string): number {
    if (this.config.alwaysInclude?.includes(type)) {
      return 1.0;
    }
    return this.config.rates[type] ?? this.config.defaultRate;
  }

  // ═══════════════════════════════════════════════════════════════════
  // Private Methods
  // ═══════════════════════════════════════════════════════════════════

  private validateConfig(): void {
    // Validate default rate
    if (this.config.defaultRate < 0 || this.config.defaultRate > 1) {
      console.warn(
        `[SamplingManager] Invalid defaultRate: ${this.config.defaultRate}. ` +
        `Must be between 0 and 1. Using 1.0.`
      );
      this.config.defaultRate = 1.0;
    }

    // Validate per-type rates
    for (const [type, rate] of Object.entries(this.config.rates)) {
      if (rate < 0 || rate > 1) {
        console.warn(
          `[SamplingManager] Invalid rate for type "${type}": ${rate}. ` +
          `Must be between 0 and 1. Using 1.0.`
        );
        this.config.rates[type] = 1.0;
      }
    }
  }
}
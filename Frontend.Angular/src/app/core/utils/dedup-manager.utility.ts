// core/utils/dedup-manager.utility.ts
/**
 * Generic Deduplication Manager
 * ═══════════════════════════════════════════════════════════════════════
 * Reusable time-window based deduplication
 * 
 * Used by: LoggerService, ToastManager, NetworkService
 * 
 * Features:
 * ✅ Generic type support
 * ✅ Configurable time window
 * ✅ Automatic cache cleanup
 * ✅ Memory leak prevention
 * ✅ Statistics tracking
 */

export interface DedupConfig<T> {
  windowMs: number;
  maxCacheSize: number;
  hashFn: (item: T) => string;
  onDuplicate?: (item: T, count: number) => void;
  cleanupIntervalMs?: number;
}

export interface DedupStats {
  totalChecks: number;
  duplicates: number;
  unique: number;
  hitRate: number;
  cacheSize: number;
  cacheFull: boolean;
  oldestEntryAge: number;
}

interface CacheEntry {
  hash: string;
  firstSeen: number;
  lastSeen: number;
  count: number;
}

export class DedupManager<T> {
  private readonly config: Required<DedupConfig<T>>;
  private cache = new Map<string, CacheEntry>();
  private cleanupTimer?: ReturnType<typeof setInterval>;
  private stats = { totalChecks: 0, duplicates: 0, unique: 0 };

  constructor(config: DedupConfig<T>) {
    this.config = {
      windowMs: config.windowMs,
      maxCacheSize: config.maxCacheSize,
      hashFn: config.hashFn,
      onDuplicate: config.onDuplicate || (() => {}),
      cleanupIntervalMs: config.cleanupIntervalMs || config.windowMs
    };

    this.startCleanupTimer();
  }

  check(item: T): boolean {
    this.stats.totalChecks++;

    const hash = this.config.hashFn(item);
    const now = Date.now();
    const existing = this.cache.get(hash);

    if (existing) {
      const age = now - existing.firstSeen;
      if (age > this.config.windowMs) {
        this.cache.delete(hash);
        this.addNewEntry(hash, now);
        this.stats.unique++;
        return false;
      }

      existing.lastSeen = now;
      existing.count++;
      this.stats.duplicates++;
      this.config.onDuplicate(item, existing.count);
      return true;
    }

    this.addNewEntry(hash, now);
    this.stats.unique++;
    return false;
  }

  isDuplicate(item: T): boolean {
    const hash = this.config.hashFn(item);
    const existing = this.cache.get(hash);
    if (!existing) return false;

    const age = Date.now() - existing.firstSeen;
    return age <= this.config.windowMs;
  }

  clear(): void {
    this.cache.clear();
    this.stats = { totalChecks: 0, duplicates: 0, unique: 0 };
  }

  getStats(): DedupStats {
    const now = Date.now();
    let oldestAge = 0;

    if (this.cache.size > 0) {
      const ages = Array.from(this.cache.values()).map(e => now - e.firstSeen);
      oldestAge = Math.max(...ages);
    }

    return {
      totalChecks: this.stats.totalChecks,
      duplicates: this.stats.duplicates,
      unique: this.stats.unique,
      hitRate: this.stats.totalChecks > 0 
        ? (this.stats.duplicates / this.stats.totalChecks) * 100 
        : 0,
      cacheSize: this.cache.size,
      cacheFull: this.cache.size >= this.config.maxCacheSize,
      oldestEntryAge: oldestAge
    };
  }

  destroy(): void {
    this.stopCleanupTimer();
    this.clear();
  }

  private addNewEntry(hash: string, now: number): void {
    if (this.cache.size >= this.config.maxCacheSize) {
      this.evictOldest();
    }

    this.cache.set(hash, { hash, firstSeen: now, lastSeen: now, count: 1 });
  }

  private evictOldest(): void {
    let oldestKey: string | null = null;
    let oldestTime = Infinity;

    for (const [key, entry] of this.cache.entries()) {
      if (entry.firstSeen < oldestTime) {
        oldestTime = entry.firstSeen;
        oldestKey = key;
      }
    }

    if (oldestKey) {
      this.cache.delete(oldestKey);
    }
  }

  private startCleanupTimer(): void {
    this.cleanupTimer = setInterval(() => {
      this.cleanup();
    }, this.config.cleanupIntervalMs);
  }

  private stopCleanupTimer(): void {
    if (this.cleanupTimer) {
      clearInterval(this.cleanupTimer);
      this.cleanupTimer = undefined;
    }
  }

  private cleanup(): void {
    const now = Date.now();
    const keysToDelete: string[] = [];

    for (const [key, entry] of this.cache.entries()) {
      const age = now - entry.firstSeen;
      if (age > this.config.windowMs) {
        keysToDelete.push(key);
      }
    }

    keysToDelete.forEach(key => this.cache.delete(key));
  }
}
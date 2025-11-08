/**
 * Optimized Entity Cache
 * ═══════════════════════════════════════════════════════════════════════
 * Phase 2 Performance Improvements:
 *   ✅ TTL-based auto-cleanup timer (prevents memory leaks)
 *   ✅ Lazy eviction (only on size check, not every write)
 *   ✅ LRU eviction strategy (remove least recently used)
 *   ✅ Proper cleanup on destroy
 * 
 * Configuration options for EntityCache
 */
export interface CacheConfig {
  /** Time-to-live for items in milliseconds (default: 5 minutes) */
  ttl?: number;

  /** Maximum number of items to keep in cache (default: 100) */
  maxSize?: number;

  /** If true, refresh TTL each time an item is accessed */
  slidingTtl?: boolean;

  /** Auto-cleanup interval in milliseconds (default: 5 minutes) */
  cleanupInterval?: number;

  /** Enable LRU eviction (default: true) */
  useLRU?: boolean;
}

interface CachedItem<T> {
  data: T;
  timestamp: number;
  lastAccessed: number; // For LRU
}

interface ListCache<T> {
  data: T[];
  timestamp: number;
}

/**
 * Generic in-memory cache for entities with IDs.
 * ─────────────────────────────────────────────────────────────
 * ✅ TTL expiration per item
 * ✅ Optional sliding TTL (extends life on access)
 * ✅ Size-bounded eviction with LRU strategy
 * ✅ Separate list cache for bulk queries
 * ✅ Automatic cleanup timer (prevents memory leaks)
 * ✅ Lazy eviction (only when needed)
 * 
 * PERFORMANCE IMPROVEMENTS:
 *   - Cleanup only runs periodically (not every write)
 *   - Size check is O(1) (just checks cache.size)
 *   - LRU eviction removes least recently used (not oldest)
 */
export class EntityCache<T extends { id: number }> {
  private cache = new Map<number, CachedItem<T>>();
  private listCache: ListCache<T> | null = null;

  private readonly defaultTtl = 5 * 60 * 1000; // 5 minutes
  private readonly defaultMaxSize = 100;
  private readonly defaultCleanupInterval = 5 * 60 * 1000; // 5 minutes

  private cleanupTimer?: ReturnType<typeof setInterval>;

  constructor(private readonly config: CacheConfig = {}) {
    this.startAutoCleanup();
  }

  // ─────────────────────────────────────────────────────────
  // 🔹 Single-item cache operations
  // ─────────────────────────────────────────────────────────

  /** Get cached item by ID (returns null if missing or expired) */
  get(id: number): T | null {
    const cached = this.cache.get(id);
    if (!cached) return null;

    if (this.isExpired(cached.timestamp)) {
      this.cache.delete(id);
      return null;
    }

    // Update access tracking for LRU
    cached.lastAccessed = Date.now();

    // Sliding TTL: refresh timestamp
    if (this.config.slidingTtl) {
      cached.timestamp = Date.now();
    }

    return cached.data;
  }

  /** Store a single item in the cache */
  set(id: number, data: T): void {
    // Lazy eviction: only check size when adding
    if (this.cache.size >= (this.config.maxSize ?? this.defaultMaxSize)) {
      this.evictOne();
    }

    const now = Date.now();
    this.cache.set(id, {
      data,
      timestamp: now,
      lastAccessed: now
    });
  }

  /** Remove a single item by ID */
  invalidate(id: number): void {
    this.cache.delete(id);
  }

  // ─────────────────────────────────────────────────────────
  // 🔹 List-level cache operations
  // ─────────────────────────────────────────────────────────

  /** Cache a list of entities (e.g., paginated result) */
  setList(data: readonly T[]): void {
    this.listCache = { data: [...data], timestamp: Date.now() };
  }

  /** Get cached list (returns null if expired or missing) */
  getList(): T[] | null {
    if (!this.listCache) return null;

    if (this.isExpired(this.listCache.timestamp)) {
      this.listCache = null;
      return null;
    }

    if (this.config.slidingTtl) {
      this.listCache.timestamp = Date.now();
    }

    return this.listCache.data;
  }

  /** Invalidate list cache */
  invalidateList(): void {
    this.listCache = null;
  }

  // ─────────────────────────────────────────────────────────
  // Maintenance / diagnostics
  // ─────────────────────────────────────────────────────────

  /**
   * Clears all cached entries (items + list)
   * Creates new Map reference to prevent race conditions
   */
  clear(): void {
    const oldSize = this.cache.size;
    this.cache = new Map<number, CachedItem<T>>();
    this.listCache = null;

    if (oldSize > 0) {
      console.debug(`[EntityCache] Cleared ${oldSize} cached items`);
    }
  }

  /**
   * Destroy cache and stop cleanup timer
   * Call this when service is destroyed
   */
  destroy(): void {
    this.stopAutoCleanup();
    this.clear();
  }

  /** Returns summary information for debugging */
  getStats() {
    return {
      itemCount: this.cache.size,
      hasListCache: !!this.listCache,
      config: this.config,
      nextCleanup: this.cleanupTimer ? 'active' : 'stopped'
    };
  }

  // ─────────────────────────────────────────────────────────
  // 🔹 Internal helpers
  // ─────────────────────────────────────────────────────────

  /** Checks if an entry is expired based on TTL */
  private isExpired(timestamp: number): boolean {
    const ttl = this.config.ttl ?? this.defaultTtl;
    return Date.now() - timestamp > ttl;
  }

  /**
   * Evict one item from cache
   * Uses LRU (Least Recently Used) strategy if enabled, otherwise FIFO
   */
  private evictOne(): void {
    if (this.cache.size === 0) return;

    const useLRU = this.config.useLRU ?? true;

    if (useLRU) {
      // Find least recently accessed item
      let lruKey: number | null = null;
      let lruTime = Infinity;

      for (const [key, item] of this.cache.entries()) {
        if (item.lastAccessed < lruTime) {
          lruTime = item.lastAccessed;
          lruKey = key;
        }
      }

      if (lruKey !== null) {
        this.cache.delete(lruKey);
        console.debug(`[EntityCache] LRU evicted item ${lruKey}`);
      }
    } else {
      // FIFO: Remove first (oldest) entry
      const oldestKey = this.cache.keys().next().value;
      if (oldestKey !== undefined) {
        this.cache.delete(oldestKey);
        console.debug(`[EntityCache] FIFO evicted item ${oldestKey}`);
      }
    }
  }

  /**
   * Start automatic cleanup timer
   * Runs periodically to remove expired entries
   */
  private startAutoCleanup(): void {
    const interval = this.config.cleanupInterval ?? this.defaultCleanupInterval;

    this.cleanupTimer = setInterval(() => {
      this.removeExpiredEntries();
    }, interval);
  }

  /**
   * Stop automatic cleanup timer
   */
  private stopAutoCleanup(): void {
    if (this.cleanupTimer) {
      clearInterval(this.cleanupTimer);
      this.cleanupTimer = undefined;
    }
  }

  /**
   * Remove all expired entries from cache
   * Called periodically by cleanup timer
   */
  private removeExpiredEntries(): void {
    const now = Date.now();
    const ttl = this.config.ttl ?? this.defaultTtl;
    let removedCount = 0;

    for (const [id, cached] of this.cache.entries()) {
      if (now - cached.timestamp > ttl) {
        this.cache.delete(id);
        removedCount++;
      }
    }

    // Also check list cache
    if (this.listCache && now - this.listCache.timestamp > ttl) {
      this.listCache = null;
      removedCount++;
    }

    if (removedCount > 0) {
      console.debug(`[EntityCache] Auto-cleanup removed ${removedCount} expired entries`);
    }
  }
}
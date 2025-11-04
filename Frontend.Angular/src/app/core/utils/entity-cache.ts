/**
 * Configuration options for EntityCache
 */
export interface CacheConfig {
  /** Time-to-live for items in milliseconds (default: 5 minutes) */
  ttl?: number;

  /** Maximum number of items to keep in cache (default: 100) */
  maxSize?: number;

  /** If true, refresh TTL each time an item is accessed */
  slidingTtl?: boolean;
}

interface CachedItem<T> {
  data: T;
  timestamp: number;
}

interface ListCache<T> {
  data: T[];
  timestamp: number;
}

/**
 * Generic in-memory cache for entities with IDs.
 * ─────────────────────────────────────────────────────────────
 * TTL expiration per item
 * Optional sliding TTL (extends life on access)
 * Size-bounded eviction
 * Separate list cache for bulk queries
 * 
 * ENHANCEMENT: Improved clear() to prevent race conditions
 */
export class EntityCache<T extends { id: number }> {
  private cache = new Map<number, CachedItem<T>>();
  private listCache: ListCache<T> | null = null;

  private readonly defaultTtl = 5 * 60 * 1000; // 5 minutes
  private readonly defaultMaxSize = 100;

  constructor(private readonly config: CacheConfig = {}) {}

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

    if (this.config.slidingTtl) cached.timestamp = Date.now();
    return cached.data;
  }

  /** Store a single item in the cache */
  set(id: number, data: T): void {
    this.ensureSizeLimit();
    this.cache.set(id, { data, timestamp: Date.now() });
  }

  /** Remove a single item by ID */
  invalidate(id: number): void {
    this.cache.delete(id);
  }

  // ─────────────────────────────────────────────────────────
  // 🔹 List-level cache operations
  // ─────────────────────────────────────────────────────────

  /** Cache a list of entities (e.g., paginated result) */
  setList(data: T[]): void {
    this.listCache = { data, timestamp: Date.now() };
  }

  /** Get cached list (returns null if expired or missing) */
  getList(): T[] | null {
    if (!this.listCache) return null;

    if (this.isExpired(this.listCache.timestamp)) {
      this.listCache = null;
      return null;
    }

    if (this.config.slidingTtl) this.listCache.timestamp = Date.now();
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
   * 
   * ENHANCEMENT: Creates new Map reference to prevent race conditions
   * 
   * WHY: If a component is iterating over cached items during clear(),
   * mutating the same Map can cause inconsistent state. Creating a
   * new reference ensures clean separation.
   */
  clear(): void {
    // Create new Map instead of calling .clear()
    // This prevents potential issues if code is iterating over the old map
    const oldSize = this.cache.size;
    this.cache = new Map<number, CachedItem<T>>();
    this.listCache = null;

    // Optional: Log for debugging (can be removed in production)
    if (oldSize > 0) {
      console.debug(`[EntityCache] Cleared ${oldSize} cached items`);
    }
  }

  /** Returns summary information for debugging */
  getStats() {
    return {
      itemCount: this.cache.size,
      hasListCache: !!this.listCache,
      config: this.config
    };
  }

  // ─────────────────────────────────────────────────────────
  // Internal helpers
  // ─────────────────────────────────────────────────────────

  /** Checks if an entry is expired based on TTL */
  private isExpired(timestamp: number): boolean {
    const ttl = this.config.ttl ?? this.defaultTtl;
    return Date.now() - timestamp > ttl;
  }

  /** Evicts oldest entries when the cache exceeds its max size */
  private ensureSizeLimit(): void {
    const maxSize = this.config.maxSize ?? this.defaultMaxSize;

    while (this.cache.size >= maxSize) {
      const oldestKey = this.cache.keys().next().value;
      if (oldestKey !== undefined) {
        this.cache.delete(oldestKey);
      } else {
        break; // safety guard if the map is empty
      }
    }
  }
}
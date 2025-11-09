// core/utils/buffer-manager.utility.ts
/**
 * Generic Buffer Manager
 * ═══════════════════════════════════════════════════════════════════════
 * Reusable buffer with automatic overflow handling
 * Used by: LoggerService, LoadingService
 */

export interface BufferConfig {
  maxSize: number;
  onOverflow?: (droppedCount: number) => void;
}

export interface BufferDiagnostics {
  size: number;
  maxSize: number;
  utilizationPercent: number;
}

export class BufferManager<T> {
  private buffer: T[] = [];
  private readonly config: Required<BufferConfig>;

  constructor(config: BufferConfig) {
    this.config = {
      maxSize: config.maxSize,
      onOverflow: config.onOverflow || (() => {})
    };
  }

  add(item: T): void {
    this.buffer.push(item);

    if (this.buffer.length > this.config.maxSize) {
      const droppedCount = this.buffer.length - this.config.maxSize;
      this.buffer = this.buffer.slice(-this.config.maxSize);
      this.config.onOverflow(droppedCount);
    }
  }

  addMany(items: T[]): void {
    items.forEach(item => this.add(item));
  }

  flush(): T[] {
    const items = this.buffer;
    this.buffer = [];
    return items;
  }

  peek(): readonly T[] {
    return [...this.buffer];
  }

  clear(): void {
    this.buffer = [];
  }

  isEmpty(): boolean {
    return this.buffer.length === 0;
  }

  size(): number {
    return this.buffer.length;
  }

  isFull(): boolean {
    return this.buffer.length >= this.config.maxSize;
  }

  getDiagnostics(): BufferDiagnostics {
    return {
      size: this.buffer.length,
      maxSize: this.config.maxSize,
      utilizationPercent: (this.buffer.length / this.config.maxSize) * 100
    };
  }
}
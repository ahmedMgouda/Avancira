import { Injectable } from '@angular/core';

/**
 * ═══════════════════════════════════════════════════════════════════════
 * CONFIG SERVICE - UPDATED
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * FIXES:
 * ✅ Added explicit ensureInitialized() method
 * ✅ No more relying on constructor side effects
 * ✅ Clearer initialization contract
 */
@Injectable({ providedIn: 'root' })
export class ConfigService {
  private initialized = false;
  private initPromise: Promise<void> | null = null;

  constructor() {
    // Constructor no longer does heavy initialization
  }

  /**
   * Explicit initialization method
   * Can be called multiple times safely (returns same promise)
   */
  async ensureInitialized(): Promise<void> {
    if (this.initialized) {
      return;
    }

    if (this.initPromise) {
      return this.initPromise;
    }

    this.initPromise = this.doInitialize();
    await this.initPromise;
  }

  private async doInitialize(): Promise<void> {
    // Load configuration from server/environment
    // TODO: Implement actual config loading logic
    
    this.initialized = true;
  }

  // ... rest of ConfigService methods
}

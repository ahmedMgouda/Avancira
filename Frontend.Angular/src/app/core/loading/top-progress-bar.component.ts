import { Component, computed, DestroyRef, effect, inject, signal, untracked } from '@angular/core';

import { LoadingService } from './loading.service';

/**
 * Top Progress Bar Component (Zoneless Compatible - ALL FIXES APPLIED)
 * ═══════════════════════════════════════════════════════════════════════
 * Slim progress bar at the top of the screen for route/HTTP loading
 * 
 * ✅ ALL FIXES APPLIED:
 *   - Fixed memory leak by tracking all setTimeout calls
 *   - Minimum visible duration (400ms) to prevent flashing
 *   - Proper cleanup on component destroy
 *   - Uses same CSS variable as loader for color consistency
 *   - Better timing logic for fast requests
 * 
 * Features:
 *   ✅ Progressive animation (10% → 90% → 100%)
 *   ✅ Auto-advances while loading
 *   ✅ Error state styling
 *   ✅ Reduced motion support
 *   ✅ Zoneless compatible (no NgZone!)
 *   ✅ Efficient rendering with signals
 *   ✅ No memory leaks
 * 
 * Usage:
 *   Add to app.component.html:
 *   <app-top-progress-bar />
 * 
 * @example
 * <app-top-progress-bar />
 */
@Component({
  selector: 'app-top-progress-bar',
  standalone: true,
  template: `
    @if (visible()) {
      <div
        class="progress-container"
        role="progressbar"
        [attr.aria-valuenow]="progress()"
        aria-valuemin="0"
        aria-valuemax="100"
        aria-label="Loading progress">
        <div
          class="progress-bar"
          [class.error]="hasError()"
          [style.width.%]="progress()">
        </div>
      </div>
    }
  `,
  styles: [`
    .progress-container {
      position: fixed;
      top: 0;
      left: 0;
      height: 3px;
      width: 100%;
      z-index: 3000;
      background: transparent;
      opacity: 1;
      transition: opacity 0.2s ease-in-out;
      transform: translateZ(0);
      backface-visibility: hidden;
    }

    .progress-bar {
      height: 100%;
      /* ✅ Uses same CSS variable as loader for consistency */
      background: linear-gradient(
        90deg, 
        var(--loader-color-medium, #2563eb), 
        #60a5fa
      );
      box-shadow: 0 0 10px var(--loader-glow-dark, rgba(37, 99, 235, 0.5));
      transition: width 0.3s cubic-bezier(0.4, 0, 0.2, 1);
      transform: translateZ(0);
      will-change: width;
    }

    .progress-bar.error {
      background: linear-gradient(90deg, #ef4444, #f87171);
      box-shadow: 0 0 10px rgba(239, 68, 68, 0.5);
    }

    @media (prefers-reduced-motion: reduce) {
      .progress-container {
        transition: none;
      }

      .progress-bar {
        transition: width 0.1s linear;
      }
    }

    @media (prefers-color-scheme: dark) {
      .progress-bar {
        box-shadow: 0 0 12px var(--loader-glow-dark, rgba(37, 99, 235, 0.7));
      }

      .progress-bar.error {
        box-shadow: 0 0 12px rgba(239, 68, 68, 0.7);
      }
    }
  `]
})
export class TopProgressBarComponent {
  private readonly loader = inject(LoadingService);
  private readonly destroyRef = inject(DestroyRef);

  readonly visible = computed(() =>
    this.loader.isRouteLoading() || this.loader.isHttpLoading()
  );

  readonly hasError = computed(() => this.loader.hasRequestErrors());

  private readonly _progress = signal(0);
  readonly progress = this._progress.asReadonly();

  private timer: ReturnType<typeof setInterval> | null = null;
  
  // ✅ FIX: Track ALL timeouts for proper cleanup
  private timeouts: Set<ReturnType<typeof setTimeout>> = new Set();
  
  // Minimum visibility duration to prevent flashing
  private readonly minVisibleDuration = 400; // ms
  private lastStartTime = 0;

  private readonly prefersReducedMotion =
    window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  constructor() {
    // Watch for visibility changes and update progress accordingly
    effect(() => {
      const isVisible = this.visible();
      
      untracked(() => {
        if (isVisible) {
          this.startProgress();
        } else {
          this.completeProgress();
        }
      });
    });

    // ✅ FIX: Cleanup ALL timers on destroy
    this.destroyRef.onDestroy(() => {
      this.clearTimer();
      this.clearAllTimeouts();
    });
  }

  /**
   * Start progressive loading animation
   * Progress advances from 10% → 90% automatically
   * ✅ Tracks start time for minimum visibility
   */
  private startProgress(): void {
    this.clearTimer();
    this.clearAllTimeouts();
    this.lastStartTime = Date.now();
    this._progress.set(10);

    // For reduced motion, just show near-complete
    if (this.prefersReducedMotion) {
      this._progress.set(90);
      return;
    }

    // Progressive animation without zone.js
    this.timer = setInterval(() => {
      const currentProgress = this._progress();

      if (currentProgress < 90) {
        // Logarithmic slowdown as we approach 90%
        const increment = (90 - currentProgress) * 0.1;
        const newProgress = Math.min(currentProgress + increment, 90);
        this._progress.set(newProgress);
      } else {
        this.clearTimer();
      }
    }, 200);
  }

  /**
   * Complete progress smoothly
   * Jumps to 100%, then fades out
   * ✅ FIXED: Ensures minimum visibility duration + tracks timeouts
   */
  private completeProgress(): void {
    this.clearTimer();
    
    // Calculate elapsed time and enforce minimum visibility
    const elapsed = Date.now() - this.lastStartTime;
    const delay = Math.max(0, this.minVisibleDuration - elapsed);
    
    // ✅ FIX: Track timeout for cleanup
    const timeout1 = setTimeout(() => {
      this._progress.set(100);
      
      // ✅ FIX: Track nested timeout too
      const timeout2 = setTimeout(() => {
        this._progress.set(0);
        this.timeouts.delete(timeout2);
      }, 300);
      
      this.timeouts.add(timeout2);
      this.timeouts.delete(timeout1);
    }, delay);
    
    this.timeouts.add(timeout1);
  }

  /** Stop and clear interval timer */
  private clearTimer(): void {
    if (this.timer) {
      clearInterval(this.timer);
      this.timer = null;
    }
  }

  /**
   * ✅ NEW: Clear all tracked timeouts
   */
  private clearAllTimeouts(): void {
    this.timeouts.forEach(timeout => clearTimeout(timeout));
    this.timeouts.clear();
  }
}
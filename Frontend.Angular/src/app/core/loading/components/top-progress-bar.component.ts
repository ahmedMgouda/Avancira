import { Component, computed, DestroyRef, effect, inject, signal, untracked } from '@angular/core';

import { LoadingService } from '../services/loading.service';

@Component({
  selector: 'app-top-progress-bar',
  standalone: true,
  template: `
    @if (visible()) {
      <div
        class="progress-container"
        [class.completing]="isCompleting()"
        role="progressbar"
        [attr.aria-valuenow]="progress()"
        aria-valuemin="0"
        aria-valuemax="100">
        <div 
          class="progress-bar"
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
      width: 100%;
      height: 3px;
      z-index: 9998;
      background: transparent;
      pointer-events: none;
    }
    
    .progress-bar {
      height: 100%;
      background: linear-gradient(90deg, var(--color-progress-start, #2563eb), var(--color-progress-end, #60a5fa));
      box-shadow: 0 0 10px var(--color-progress-shadow, rgba(37, 99, 235, 0.5));
      transition: width 400ms cubic-bezier(0.4, 0, 0.2, 1);
      will-change: width;
      transform-origin: left;
    }
    
    .progress-container.completing .progress-bar {
      transition: width 200ms ease-out;
    }
    
    @media (prefers-reduced-motion: reduce) {
      .progress-bar {
        transition: width 150ms linear;
      }
    }
    
    :host-context(.dark-theme) .progress-bar {
      box-shadow: 0 0 12px var(--color-progress-shadow, rgba(59, 130, 246, 0.6));
    }
  `]
})
export class TopProgressBarComponent {
  private readonly loader = inject(LoadingService);
  private readonly destroyRef = inject(DestroyRef);

  // Configuration
  private readonly MIN_VISIBLE_MS = 300;
  private readonly COMPLETE_DELAY_MS = 150;
  
  // State
  private readonly _progress = signal(0);
  private readonly _isCompleting = signal(false);
  
  readonly progress = this._progress.asReadonly();
  readonly isCompleting = this._isCompleting.asReadonly();
  
  readonly visible = computed(() => 
    this.loader.isRouteLoading() || this.loader.isHttpLoading()
  );

  // Tracking
  private startTime = 0;
  private peakCount = 0;
  private completedCount = 0;
  private completeTimer?: ReturnType<typeof setTimeout>;
  private resetTimer?: ReturnType<typeof setTimeout>;

  constructor() {
    effect(() => {
      const httpActive = this.loader.activeCount();
      const routeActive = this.loader.isRouteLoading();
      
      untracked(() => this.updateProgress(httpActive, routeActive));
    });

    this.destroyRef.onDestroy(() => this.cleanup());
  }

  private updateProgress(httpActive: number, routeActive: boolean): void {
    const totalActive = httpActive + (routeActive ? 1 : 0);

    if (totalActive > 0) {
      // Clear any pending completion
      this.clearTimers();
      
      // Initialize on first request
      if (this.startTime === 0) {
        this.startTime = Date.now();
        this._progress.set(10);
        this._isCompleting.set(false);
      }
      
      // Track peak for completion calculation
      if (totalActive > this.peakCount) {
        this.peakCount = totalActive;
      }
      
      // Calculate completed work
      const newCompleted = this.peakCount - totalActive;
      
      // Only advance progress (never go backward)
      if (newCompleted > this.completedCount) {
        this.completedCount = newCompleted;
        const ratio = this.completedCount / this.peakCount;
        const newProgress = 10 + (ratio * 80);
        this._progress.set(Math.min(90, newProgress));
      }
    } else {
      // All work complete
      this.complete();
    }
  }

  private complete(): void {
    if (this._isCompleting()) return;
    
    this._isCompleting.set(true);
    
    // Ensure minimum visible time
    const elapsed = Date.now() - this.startTime;
    const delay = Math.max(0, this.MIN_VISIBLE_MS - elapsed);
    
    this.completeTimer = setTimeout(() => {
      this._progress.set(100);
      
      this.resetTimer = setTimeout(() => {
        this.reset();
      }, this.COMPLETE_DELAY_MS);
    }, delay);
  }

  private reset(): void {
    this._progress.set(0);
    this._isCompleting.set(false);
    this.startTime = 0;
    this.peakCount = 0;
    this.completedCount = 0;
  }

  private clearTimers(): void {
    if (this.completeTimer) {
      clearTimeout(this.completeTimer);
      this.completeTimer = undefined;
    }
    if (this.resetTimer) {
      clearTimeout(this.resetTimer);
      this.resetTimer = undefined;
    }
  }

  private cleanup(): void {
    this.clearTimers();
  }
}
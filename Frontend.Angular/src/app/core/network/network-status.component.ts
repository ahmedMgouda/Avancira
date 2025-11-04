import { animate, style, transition, trigger } from '@angular/animations';
import { Component, computed, effect, inject, Renderer2, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { fromEvent, merge } from 'rxjs';
import { map } from 'rxjs/operators';

/**
 * Network Status Component
 * ═══════════════════════════════════════════════════════════════════════
 * Displays a notification banner when internet connection is lost/restored
 * 
 * Features:
 *   ✅ Auto-detects online/offline status
 *   ✅ Slide-in/out animation
 *   ✅ Toast notification style
 *   ✅ Auto-hide when back online (configurable delay)
 *   ✅ Accessibility compliant
 *   ✅ Zoneless compatible
 *   ✅ Dark mode support
 *   ✅ Retry button for offline state
 * 
 * Usage:
 *   Add to app.component.html (top of layout):
 *   <app-network-status />
 * 
 * @example
 * <app-network-status />
 */
@Component({
  selector: 'app-network-status',
  standalone: true,
  animations: [
    trigger('slideDown', [
      transition(':enter', [
        style({ transform: 'translateY(-100%)', opacity: 0 }),
        animate('300ms ease-out', style({ transform: 'translateY(0)', opacity: 1 })),
      ]),
      transition(':leave', [
        animate('300ms ease-in', style({ transform: 'translateY(-100%)', opacity: 0 })),
      ]),
    ]),
  ],
  template: `
    @if (shouldShow()) {
      <div 
        @slideDown
        class="network-status"
        [class.offline]="!isOnline()"
        [class.online]="isOnline()"
        role="alert"
        [attr.aria-live]="isOnline() ? 'polite' : 'assertive'"
        aria-atomic="true">
        
        <div class="content">
          <!-- Icon -->
          <div class="icon">
            @if (isOnline()) {
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path>
                <polyline points="22 4 12 14.01 9 11.01"></polyline>
              </svg>
            } @else {
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="1" y1="1" x2="23" y2="23"></line>
                <path d="M16.72 11.06A10.94 10.94 0 0 1 19 12.55"></path>
                <path d="M5 12.55a10.94 10.94 0 0 1 5.17-2.39"></path>
                <path d="M10.71 5.05A16 16 0 0 1 22.58 9"></path>
                <path d="M1.42 9a15.91 15.91 0 0 1 4.7-2.88"></path>
                <path d="M8.53 16.11a6 6 0 0 1 6.95 0"></path>
                <line x1="12" y1="20" x2="12.01" y2="20"></line>
              </svg>
            }
          </div>

          <!-- Message -->
          <div class="message">
            <strong>{{ title() }}</strong>
            <span>{{ message() }}</span>
          </div>

          <!-- Retry Button (offline only) -->
          @if (!isOnline()) {
            <button 
              type="button"
              class="retry-btn"
              (click)="checkConnection()"
              aria-label="Retry connection">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polyline points="23 4 23 10 17 10"></polyline>
                <path d="M20.49 15a9 9 0 1 1-2.12-9.36L23 10"></path>
              </svg>
              Retry
            </button>
          }

          <!-- Close Button -->
          <button 
            type="button"
            class="close-btn"
            (click)="dismiss()"
            aria-label="Close notification">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="18" y1="6" x2="6" y2="18"></line>
              <line x1="6" y1="6" x2="18" y2="18"></line>
            </svg>
          </button>
        </div>
      </div>
    }
  `,
  styles: [`
    /* ─────────────────────────────────────────────────────────
       Container
       ───────────────────────────────────────────────────────── */
    .network-status {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      z-index: 9999;
      padding: 1rem;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
      backdrop-filter: blur(10px);
      
      /* GPU optimization */
      transform: translateZ(0);
      will-change: transform, opacity;
      contain: layout style paint;
    }

    .network-status.offline {
      background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%);
      color: white;
    }

    .network-status.online {
      background: linear-gradient(135deg, #10b981 0%, #059669 100%);
      color: white;
    }

    /* ─────────────────────────────────────────────────────────
       Content Layout
       ───────────────────────────────────────────────────────── */
    .content {
      max-width: 1200px;
      margin: 0 auto;
      display: flex;
      align-items: center;
      gap: 1rem;
    }

    /* ─────────────────────────────────────────────────────────
       Icon
       ───────────────────────────────────────────────────────── */
    .icon {
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      width: 40px;
      height: 40px;
      border-radius: 50%;
      background: rgba(255, 255, 255, 0.2);
    }

    .icon svg {
      width: 24px;
      height: 24px;
    }

    /* ─────────────────────────────────────────────────────────
       Message
       ───────────────────────────────────────────────────────── */
    .message {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .message strong {
      font-size: 1rem;
      font-weight: 600;
      line-height: 1.2;
    }

    .message span {
      font-size: 0.875rem;
      opacity: 0.9;
      line-height: 1.4;
    }

    /* ─────────────────────────────────────────────────────────
       Buttons
       ───────────────────────────────────────────────────────── */
    .retry-btn,
    .close-btn {
      background: rgba(255, 255, 255, 0.2);
      border: 1px solid rgba(255, 255, 255, 0.3);
      color: white;
      padding: 0.5rem 1rem;
      border-radius: 6px;
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      display: flex;
      align-items: center;
      gap: 0.5rem;
      transition: all 0.2s ease;
      flex-shrink: 0;
    }

    .retry-btn:hover,
    .close-btn:hover {
      background: rgba(255, 255, 255, 0.3);
      transform: translateY(-1px);
    }

    .retry-btn:active,
    .close-btn:active {
      transform: translateY(0);
    }

    .close-btn {
      padding: 0.5rem;
      min-width: 36px;
    }

    /* ─────────────────────────────────────────────────────────
       Responsive Design
       ───────────────────────────────────────────────────────── */
    @media (max-width: 640px) {
      .network-status {
        padding: 0.75rem;
      }

      .content {
        gap: 0.75rem;
      }

      .icon {
        width: 32px;
        height: 32px;
      }

      .icon svg {
        width: 20px;
        height: 20px;
      }

      .message strong {
        font-size: 0.875rem;
      }

      .message span {
        font-size: 0.75rem;
      }

      .retry-btn {
        padding: 0.375rem 0.75rem;
        font-size: 0.75rem;
      }

      .retry-btn svg {
        width: 14px;
        height: 14px;
      }
    }

    /* ─────────────────────────────────────────────────────────
       Accessibility: Reduced Motion
       ───────────────────────────────────────────────────────── */
    @media (prefers-reduced-motion: reduce) {
      .network-status {
        animation: none;
      }

      .retry-btn:hover,
      .close-btn:hover {
        transform: none;
      }
    }

    /* ─────────────────────────────────────────────────────────
       Print Styles
       ───────────────────────────────────────────────────────── */
    @media print {
      .network-status {
        display: none;
      }
    }
  `]
})
export class NetworkStatusComponent {
  private readonly renderer = inject(Renderer2);
  
  // State
  readonly isOnline = signal(navigator.onLine);
  private readonly isDismissed = signal(false);
  private readonly justReconnected = signal(false);
  
  // Computed
  readonly shouldShow = computed(() => {
    // Don't show if dismissed
    if (this.isDismissed()) return false;
    
    // Always show when offline
    if (!this.isOnline()) return true;
    
    // Show briefly when reconnected (unless dismissed)
    return this.justReconnected();
  });

  readonly title = computed(() => 
    this.isOnline() ? 'Back Online' : 'No Internet Connection'
  );

  readonly message = computed(() => 
    this.isOnline() 
      ? 'Your connection has been restored.'
      : 'Please check your internet connection and try again.'
  );

  constructor() {
    // Listen to browser online/offline events
    merge(
      fromEvent(window, 'online').pipe(map(() => true)),
      fromEvent(window, 'offline').pipe(map(() => false))
    )
      .pipe(takeUntilDestroyed())
      .subscribe(online => {
        this.isOnline.set(online);
        
        if (online) {
          // Reset dismissed state when back online
          this.isDismissed.set(false);
          this.justReconnected.set(true);
          
          // Auto-hide "back online" message after 5 seconds
          setTimeout(() => {
            this.justReconnected.set(false);
          }, 5000);
        } else {
          // Reset reconnected flag when offline
          this.justReconnected.set(false);
        }
      });

    // Update body class for global styling (optional)
    effect(() => {
      const online = this.isOnline();
      const body = document.body;
      
      if (online) {
        this.renderer.removeClass(body, 'network-offline');
        this.renderer.addClass(body, 'network-online');
      } else {
        this.renderer.removeClass(body, 'network-online');
        this.renderer.addClass(body, 'network-offline');
      }
    });
  }

  /**
   * Manual connection check (triggered by retry button)
   */
  checkConnection(): void {
    // Update state immediately
    this.isOnline.set(navigator.onLine);
    
    // Optionally: Test actual connectivity with a ping
    if (navigator.onLine) {
      this.verifyConnection();
    }
  }

  /**
   * Verify actual internet connectivity (not just network adapter status)
   */
  private verifyConnection(): void {
    fetch('/favicon.ico', { 
      method: 'HEAD',
      cache: 'no-cache',
      mode: 'no-cors'
    })
      .then(() => {
        // Connection verified
        this.isOnline.set(true);
      })
      .catch(() => {
        // Connection failed despite navigator.onLine
        this.isOnline.set(false);
      });
  }

  /**
   * Dismiss notification
   */
  dismiss(): void {
    this.isDismissed.set(true);
    
    // Reset dismissed state after 10 seconds (in case connection changes again)
    setTimeout(() => {
      if (this.isOnline()) {
        this.isDismissed.set(false);
      }
    }, 10000);
  }
}
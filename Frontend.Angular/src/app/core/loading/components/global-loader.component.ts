import { animate, style, transition, trigger } from '@angular/animations';
import { DOCUMENT } from '@angular/common';
import { Component, effect, inject, Renderer2 } from '@angular/core';

import { LoadingService } from '../services/loading.service';

@Component({
  selector: 'app-global-loader',
  standalone: true,
  animations: [
    trigger('fadeIn', [
      transition(':enter', [
        style({ opacity: 0 }),
        animate('200ms ease-out', style({ opacity: 1 })),
      ]),
      transition(':leave', [
        animate('150ms ease-in', style({ opacity: 0 })),
      ]),
    ]),
  ],
  template: `
    @if (isActive()) {
      <div
        @fadeIn
        class="overlay"
        role="dialog"
        aria-modal="true"
        aria-busy="true"
        [attr.aria-labelledby]="message() ? 'loading-message' : null">
        <div class="content">
          <div class="loader-wrap">
            <div class="loader" role="status" aria-label="Loading"></div>
          </div>
          @if (message()) {
            <p id="loading-message" class="message">{{ message() }}</p>
          } @else {
            <span class="sr-only">Loading, please wait...</span>
          }
        </div>
      </div>
    }
  `,
  styles: [`
    /* Overlay fills entire view */
    .overlay {
      position: fixed;
      inset: 0;
      display: flex;
      align-items: center;
      justify-content: center;
      background: rgba(255, 255, 255, 0.85);
      backdrop-filter: blur(4px);
      z-index: 9999;
    }

    /* Centers everything */
    .content {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      text-align: center;
    }

    /* Reserve space for spinner explicitly */
    .loader-wrap {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 64px;     
      height: 64px;
      margin-bottom: 1rem;
    }

    /* Spinner itself */
    .loader {
      width: 48px;
      height: 48px;
      border: 4px solid rgba(0, 0, 0, 0.1);
      border-top-color: #2563eb;
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
      box-sizing: border-box;
    }

    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }

    /* Message below spinner */
    .message {
      font-size: 15px;
      font-weight: 500;
      color: #374151;
      margin: 0;
      max-width: 300px;
      line-height: 1.5;
      text-align: center;
    }

    .sr-only {
      position: absolute;
      width: 1px;
      height: 1px;
      padding: 0;
      margin: -1px;
      overflow: hidden;
      clip: rect(0, 0, 0, 0);
      white-space: nowrap;
      border: 0;
    }

    @media (prefers-color-scheme: dark) {
      .overlay {
        background: rgba(0, 0, 0, 0.7);
      }
      .loader {
        border-color: rgba(255, 255, 255, 0.1);
        border-top-color: #60a5fa;
      }
      .message {
        color: #e5e7eb;
      }
    }
  `]
})
export class GlobalLoaderComponent {
  private readonly loader = inject(LoadingService);
  private readonly renderer = inject(Renderer2);
  private readonly document = inject(DOCUMENT);

  readonly isActive = this.loader.isGlobalLoading;
  readonly message = this.loader.globalMessage;

  constructor() {
    // Lock scroll while loader visible
    effect(() => {
      const active = this.isActive();
      const body = this.document.body;
      if (active) this.renderer.setStyle(body, 'overflow', 'hidden');
      else this.renderer.removeStyle(body, 'overflow');
    }, { allowSignalWrites: false });
  }
}

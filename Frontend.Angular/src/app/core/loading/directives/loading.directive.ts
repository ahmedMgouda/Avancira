import {
  DestroyRef,
  Directive,
  effect,
  ElementRef,
  inject,
  Injector,
  input,
  Renderer2,
  runInInjectionContext,
  untracked,
} from '@angular/core';

type LoadingSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl' | number;

@Directive({
  selector: '[loading]',
  standalone: true,
  host: {
    '[style.--loader-size]': 'getLoaderSize()',
    '[style.--loader-color]': 'color()',
    '[style.--loader-speed]': 'getLoaderSpeed()',
  }
})
export class LoadingDirective {
  // Inputs
  readonly loading = input.required<boolean>();
  readonly mode = input<'button' | 'overlay'>('button');
  readonly size = input<LoadingSize>('md');
  readonly color = input('#2563eb');
  readonly speed = input(0.8);

  private readonly el = inject(ElementRef<HTMLElement>);
  private readonly renderer = inject(Renderer2);
  private readonly injector = inject(Injector);
  private readonly destroyRef = inject(DestroyRef);

  private spinnerContainer?: HTMLElement;
  private contentWrapper?: HTMLElement;
  private styleElement?: HTMLStyleElement;

  constructor() {
    this.injectStyles();
    
    const host = this.el.nativeElement;

    runInInjectionContext(this.injector, () => {
      effect(() => {
        const isLoading = this.loading();
        untracked(() => {
          isLoading ? this.activate(host) : this.deactivate(host);
        });
      });
    });

    this.destroyRef.onDestroy(() => this.cleanup(host));
  }

  getLoaderSize(): string {
    return `${this.getSizePx()}px`;
  }

  getLoaderSpeed(): string {
    return `${this.speed()}s`;
  }

  private injectStyles(): void {
    // Check if styles already injected
    if (document.getElementById('loading-directive-styles')) return;

    const style = document.createElement('style');
    style.id = 'loading-directive-styles';
    style.textContent = `
      @keyframes loader-spin {
        to { transform: rotate(360deg); }
      }

      .loader-spinner {
        display: inline-block;
        width: var(--loader-size, 20px);
        height: var(--loader-size, 20px);
        border: 3px solid var(--color-loader-border, rgba(0, 0, 0, 0.1));
        border-top-color: var(--loader-color, var(--color-loader-border-top, #2563eb));
        border-radius: 50%;
        animation: loader-spin var(--loader-speed, 0.8s) linear infinite;
        will-change: transform;
      }

      .is-loading {
        pointer-events: none;
        cursor: not-allowed;
      }

      .loading-content {
        display: inline-block;
        transition: opacity 150ms ease, visibility 150ms ease;
      }

      .loading-container.is-loading {
        pointer-events: none;
      }

      .loading-overlay {
        background: var(--color-overlay, rgba(255, 255, 255, 0.85));
        -webkit-backdrop-filter: blur(4px);
        backdrop-filter: blur(4px);
      }

      .dark-theme .loader-spinner {
        border: 3px solid var(--color-loader-border, rgba(148, 163, 184, 0.25));
        border-top-color: var(--loader-color, var(--color-loader-border-top, #60a5fa));
      }

      .dark-theme .loading-overlay {
        background: var(--color-overlay, rgba(0, 0, 0, 0.6));
      }

      @media (prefers-reduced-motion: reduce) {
        .loader-spinner {
          animation: none;
          opacity: 0.8;
        }
        
        .loading-content {
          transition: none;
        }
        
        .loading-overlay {
          -webkit-backdrop-filter: none;
          backdrop-filter: none;
        }
      }
    `;
    
    document.head.appendChild(style);
    this.styleElement = style;
  }

  // Activation/Deactivation
  private activate(host: HTMLElement): void {
    this.renderer.setStyle(host, 'position', 'relative');

    if (this.mode() === 'button') {
      this.activateButton(host);
    } else {
      this.activateOverlay(host);
    }
  }

  private deactivate(host: HTMLElement): void {
    this.renderer.removeClass(host, 'is-loading');
    this.renderer.removeClass(host, 'loading-container');

    if (this.contentWrapper) {
      this.renderer.removeStyle(this.contentWrapper, 'visibility');
      this.renderer.removeStyle(this.contentWrapper, 'opacity');
    }

    if (this.spinnerContainer) {
      this.renderer.setStyle(this.spinnerContainer, 'display', 'none');
    }
  }

  // Button Mode
  private activateButton(host: HTMLElement): void {
    this.renderer.addClass(host, 'is-loading');

    // Wrap content if needed
    if (!this.contentWrapper) {
      this.contentWrapper = this.renderer.createElement('span');
      this.renderer.addClass(this.contentWrapper, 'loading-content');
      
      while (host.firstChild) {
        this.renderer.appendChild(this.contentWrapper, host.firstChild);
      }
      
      this.renderer.appendChild(host, this.contentWrapper);
    }

    // Hide content
    this.renderer.setStyle(this.contentWrapper, 'visibility', 'hidden');
    this.renderer.setStyle(this.contentWrapper, 'opacity', '0');

    // Show spinner
    if (!this.spinnerContainer) {
      this.spinnerContainer = this.createInlineSpinner();
      this.renderer.appendChild(host, this.spinnerContainer);
    } else {
      this.renderer.setStyle(this.spinnerContainer, 'display', 'flex');
    }
  }

  // Overlay Mode
  private activateOverlay(host: HTMLElement): void {
    this.renderer.addClass(host, 'loading-container');

    if (!this.spinnerContainer) {
      this.spinnerContainer = this.createOverlaySpinner();
      this.renderer.appendChild(host, this.spinnerContainer);
    } else {
      this.renderer.setStyle(this.spinnerContainer, 'display', 'flex');
    }
  }

  // Spinner Creation
  private createInlineSpinner(): HTMLElement {
    const container = this.renderer.createElement('span');
    this.renderer.setStyle(container, 'position', 'absolute');
    this.renderer.setStyle(container, 'inset', '0');
    this.renderer.setStyle(container, 'display', 'flex');
    this.renderer.setStyle(container, 'align-items', 'center');
    this.renderer.setStyle(container, 'justify-content', 'center');
    this.renderer.setStyle(container, 'pointer-events', 'none');

    const spinner = this.createSpinnerElement();
    this.renderer.appendChild(container, spinner);

    return container;
  }

  private createOverlaySpinner(): HTMLElement {
    const overlay = this.renderer.createElement('div');
    this.renderer.addClass(overlay, 'loading-overlay');
    this.renderer.setStyle(overlay, 'position', 'absolute');
    this.renderer.setStyle(overlay, 'inset', '0');
    this.renderer.setStyle(overlay, 'display', 'flex');
    this.renderer.setStyle(overlay, 'align-items', 'center');
    this.renderer.setStyle(overlay, 'justify-content', 'center');
    this.renderer.setStyle(overlay, 'border-radius', 'inherit');
    this.renderer.setStyle(overlay, 'z-index', '10');
    this.renderer.setStyle(overlay, 'pointer-events', 'all');

    const spinner = this.createSpinnerElement();
    this.renderer.appendChild(overlay, spinner);

    return overlay;
  }

  private createSpinnerElement(): HTMLElement {
    const spinner = this.renderer.createElement('span');
    this.renderer.addClass(spinner, 'loader-spinner');
    return spinner;
  }

  // Helpers
  private getSizePx(): number {
    const size = this.size();
    if (typeof size === 'number') return size;
    
    const sizeMap = {
      xs: 12,
      sm: 16,
      md: 20,
      lg: 28,
      xl: 36
    };
    
    return sizeMap[size] ?? 20;
  }

  private cleanup(host: HTMLElement): void {
    // Remove spinner
    if (this.spinnerContainer?.parentNode === host) {
      this.renderer.removeChild(host, this.spinnerContainer);
    }

    // Unwrap content
    if (this.contentWrapper?.parentNode === host) {
      while (this.contentWrapper.firstChild) {
        this.renderer.appendChild(host, this.contentWrapper.firstChild);
      }
      this.renderer.removeChild(host, this.contentWrapper);
    }

    this.renderer.removeClass(host, 'is-loading');
    this.renderer.removeClass(host, 'loading-container');
  }
}
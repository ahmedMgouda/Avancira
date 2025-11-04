// loading.directive.ts - COMPLETE FIXED VERSION
import {
  ChangeDetectorRef,
  computed,
  DestroyRef,
  Directive,
  effect,
  ElementRef,
  inject,
  input,
  Renderer2,
  Signal,
  untracked,
} from '@angular/core';
import { isSignal } from '@angular/core';

/**
 * Loading Directive (Zoneless Compatible - ALL FIXES APPLIED)
 * ═══════════════════════════════════════════════════════════════════════
 * Adds a loading spinner to any element (buttons or containers)
 *
 * ✅ ALL CRITICAL FIXES APPLIED:
 *   - Ultra-clear spinner with enhanced contrast and shadows
 *   - Fixed button width jump using absolute positioning
 *   - Fixed race condition with proper cleanup callbacks
 *   - Fixed null checks and edge cases
 *   - Improved color detection for all button states
 *   - Memory leak prevention
 *
 * Features:
 *   ✅ Ultra-visible spinner on all backgrounds
 *   ✅ No width jump when spinner appears
 *   ✅ Automatic button/container detection
 *   ✅ Adaptive spinner sizing and color
 *   ✅ Overlay or inline display modes
 *   ✅ Accessibility (disabled state, ARIA)
 *   ✅ Zoneless compatible
 */
@Directive({
  selector: '[appLoading]',
  standalone: true,
})
export class LoadingDirective {
  private readonly el = inject(ElementRef<HTMLElement>);
  private readonly renderer = inject(Renderer2);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);

  // Inputs
  readonly appLoading = input.required<boolean | Signal<boolean>>();
  readonly loadingMessage = input<string>();
  readonly loadingSize = input<'small' | 'medium' | 'large'>('medium');
  readonly loadingColor = input<string>();
  readonly loadingPosition = input<'left' | 'right'>('left');
  readonly loadingDisplay = input<'overlay' | 'inline'>('overlay');

  // State
  private originalState: {
    disabled: boolean;
    ariaDisabled: string | null;
    ariaBusy: string | null;
    position?: string;
    minWidth?: string;
    width?: string;
  } | null = null;

  private loadingContainer: HTMLElement | null = null;
  private elementType: 'button' | 'container' = 'container';
  private cleanupTimeoutId: ReturnType<typeof setTimeout> | null = null;
  private isCurrentlyLoading = false;

  // Computed loading state
  private readonly isLoading = computed(() => {
    const value = this.appLoading();
    return isSignal(value) ? value() : !!value;
  });

  constructor() {
    this.detectElementType();

    effect(() => {
      const loading = this.isLoading();
      untracked(() => {
        loading ? this.show() : this.hide();
      });
    });

    this.destroyRef.onDestroy(() => {
      if (this.cleanupTimeoutId) {
        clearTimeout(this.cleanupTimeoutId);
        this.cleanupTimeoutId = null;
      }
      if (this.isCurrentlyLoading) this.hide();
      this.cleanup();
    });
  }

  private detectElementType(): void {
    const el = this.el.nativeElement;
    const tag = el.tagName.toLowerCase();
    if (
      tag === 'button' ||
      tag === 'a' ||
      el.getAttribute('role') === 'button' ||
      el.classList.contains('btn') ||
      el.classList.contains('e-btn') ||
      el.classList.contains('mat-button') ||
      el.classList.contains('mat-raised-button') ||
      el.classList.contains('mat-flat-button') ||
      el.classList.contains('mat-stroked-button')
    ) {
      this.elementType = 'button';
    } else {
      this.elementType = 'container';
    }
  }

  private show(): void {
    // ✅ FIX: Immediate cleanup of pending fade-out
    if (this.cleanupTimeoutId) {
      clearTimeout(this.cleanupTimeoutId);
      this.cleanupTimeoutId = null;
      
      if (this.loadingContainer && this.loadingContainer.parentNode) {
        try {
          this.renderer.removeChild(
            this.loadingContainer.parentNode,
            this.loadingContainer
          );
        } catch (e) {
          console.debug('[LoadingDirective] Old container cleanup failed', e);
        }
      }
      this.loadingContainer = null;
    }

    if (this.isCurrentlyLoading && this.loadingContainer) {
      return;
    }

    const el = this.el.nativeElement;

    if (!this.originalState) {
      const computedStyle = window.getComputedStyle(el);
      
      this.originalState = {
        disabled: 'disabled' in el ? (el as HTMLButtonElement).disabled : false,
        ariaDisabled: el.getAttribute('aria-disabled'),
        ariaBusy: el.getAttribute('aria-busy'),
      };
      
      if (this.elementType === 'button') {
        this.originalState.width = computedStyle.width;
        this.originalState.minWidth = computedStyle.minWidth;
        
        // ✅ Lock button width to prevent jump
        if (el.offsetWidth > 0) {
          this.renderer.setStyle(el, 'min-width', `${el.offsetWidth}px`);
        }
      } else {
        this.originalState.position = computedStyle.position;
      }
    }

    this.renderer.addClass(el, 'is-loading');
    this.renderer.setAttribute(el, 'aria-busy', 'true');

    if ('disabled' in el) (el as HTMLButtonElement).disabled = true;

    this.elementType === 'button'
      ? this.showButtonLoading(el)
      : this.showContainerLoading(el);

    this.isCurrentlyLoading = true;
    this.cdr.markForCheck();
  }

  private hide(): void {
    if (!this.isCurrentlyLoading) {
      return;
    }

    const el = this.el.nativeElement;
    if (!this.originalState) return;

    const wasInline = this.loadingDisplay() === 'inline';
    
    // ✅ FIX: Pass callback to handle post-cleanup actions
    this.removeLoadingIndicator(el, () => {
      if (wasInline) {
        Array.from(el.children).forEach((child) => {
          if (child !== this.loadingContainer) {
            this.renderer.removeStyle(child, 'display');
          }
        });
      }
    });

    this.renderer.removeClass(el, 'is-loading');
    const state = this.originalState;

    // Restore ARIA
    state.ariaBusy
      ? this.renderer.setAttribute(el, 'aria-busy', state.ariaBusy)
      : this.renderer.removeAttribute(el, 'aria-busy');

    if ('disabled' in el) (el as HTMLButtonElement).disabled = state.disabled;

    state.ariaDisabled
      ? this.renderer.setAttribute(el, 'aria-disabled', state.ariaDisabled)
      : this.renderer.removeAttribute(el, 'aria-disabled');

    // ✅ Restore original dimensions
    if (this.elementType === 'button') {
      if (state.width === 'auto' || !state.width) {
        this.renderer.removeStyle(el, 'min-width');
      } else {
        this.renderer.setStyle(el, 'min-width', state.minWidth || 'auto');
      }
    } else if (state.position === 'static') {
      this.renderer.removeStyle(el, 'position');
    }

    this.isCurrentlyLoading = false;
    this.cdr.markForCheck();
  }

  /**
   * ✅ FIXED: Absolute positioned overlay prevents width jump
   */
  private showButtonLoading(el: HTMLElement): void {
    const container = this.renderer.createElement('span') as HTMLElement;
    this.loadingContainer = container;
    
    // Absolute positioning overlay
    this.renderer.addClass(container, 'loading-spinner-overlay');
    this.renderer.setStyle(container, 'position', 'absolute');
    this.renderer.setStyle(container, 'inset', '0');
    this.renderer.setStyle(container, 'display', 'flex');
    this.renderer.setStyle(container, 'align-items', 'center');
    this.renderer.setStyle(container, 'justify-content', 'center');
    this.renderer.setStyle(container, 'z-index', '10');
    this.renderer.setStyle(container, 'pointer-events', 'none');
    this.renderer.setStyle(container, 'background', 'transparent');

    const position = window.getComputedStyle(el).position;
    if (position === 'static') {
      this.renderer.setStyle(el, 'position', 'relative');
    }

    // Hide original content (but keep dimensions)
    Array.from(el.childNodes).forEach((child) => {
      if (child.nodeType === Node.ELEMENT_NODE) {
        this.renderer.setStyle(child as HTMLElement, 'visibility', 'hidden');
      }
    });

    this.renderer.appendChild(el, container);
    this.createButtonSpinner(container, el);
  }

  private showContainerLoading(el: HTMLElement): void {
    const mode = this.loadingDisplay();
    mode === 'inline' ? this.showInlineContainer(el) : this.showOverlayContainer(el);
  }

  private showInlineContainer(el: HTMLElement): void {
    Array.from(el.children).forEach((child) =>
      this.renderer.setStyle(child, 'display', 'none')
    );

    const container = this.renderer.createElement('div') as HTMLElement;
    this.loadingContainer = container;
    this.renderer.addClass(container, 'loading-inline-container');
    this.renderer.setStyle(container, 'display', 'flex');
    this.renderer.setStyle(container, 'align-items', 'center');
    this.renderer.setStyle(container, 'justify-content', 'center');
    this.renderer.setStyle(container, 'flex-direction', 'column');
    this.renderer.setStyle(container, 'width', '100%');
    this.renderer.setStyle(container, 'min-height', '100px');

    this.createSpinner(container);
    const message = this.loadingMessage();
    if (message) this.createMessageElement(container, message);

    this.renderer.appendChild(el, container);
  }

  private showOverlayContainer(el: HTMLElement): void {
    const container = this.renderer.createElement('div') as HTMLElement;
    this.loadingContainer = container;

    const pos = this.originalState?.position || window.getComputedStyle(el).position;
    if (pos === 'static') this.renderer.setStyle(el, 'position', 'relative');

    const overlayStyles = {
      position: 'absolute',
      inset: '0',
      display: 'flex',
      'flex-direction': 'column',
      'align-items': 'center',
      'justify-content': 'center',
      background: 'var(--overlay-bg, rgba(255,255,255,0.8))',
      'backdrop-filter': 'blur(2px)',
      'border-radius': 'inherit',
      'z-index': '10',
      opacity: '0',
      transition: 'opacity 0.15s ease-out',
    };

    this.renderer.addClass(container, 'loading-overlay');
    Object.entries(overlayStyles).forEach(([k, v]) =>
      this.renderer.setStyle(container, k, v)
    );

    this.createSpinner(container);
    const message = this.loadingMessage();
    if (message) this.createMessageElement(container, message);

    this.renderer.appendChild(el, container);

    setTimeout(() => {
      if (this.loadingContainer === container) {
        this.renderer.setStyle(container, 'opacity', '1');
        this.cdr.markForCheck();
      }
    }, 10);
  }

  /**
   * ✅ ULTRA-CLEAR: Create high-contrast spinner with shadow effects
   */
  private createButtonSpinner(container: HTMLElement, button: HTMLElement): void {
    const spinner = this.renderer.createElement('div') as HTMLElement;
    this.renderer.addClass(spinner, 'loader-spinner');
    this.renderer.addClass(spinner, 'loader-spinner-button');

    const style = window.getComputedStyle(button);
    const buttonHeight = button.offsetHeight;
    const fontSize = parseFloat(style.fontSize);
    const sizeInput = this.loadingSize();

    // Calculate size
    let size = 16;
    if (buttonHeight < 32) size = Math.min(14, fontSize * 0.9);
    else if (buttonHeight < 40) size = Math.min(16, fontSize);
    else size = Math.min(20, fontSize * 1.1);

    if (sizeInput === 'small') size = Math.min(size, 14);
    if (sizeInput === 'large') size = Math.max(size, 18);

    // ✅ Thicker border for better visibility
    const borderWidth = Math.max(3, Math.round(size / 6));

    // ✅ Get ultra-high-contrast colors
    const colors = this.getUltraContrastColors(button, style);

    const spinnerStyles = {
      width: `${size}px`,
      height: `${size}px`,
      border: `${borderWidth}px solid ${colors.track}`,
      'border-top-color': colors.spinner,
      'border-radius': '50%',
      animation: 'loader-spin 0.8s linear infinite',
      'flex-shrink': '0',
      position: 'relative',
      'will-change': 'transform',
      'transform-origin': 'center',
      'backface-visibility': 'hidden',
      filter: 'none',
      opacity: '1',
      // ✅ Add shadow for ultra-clarity
      'box-shadow': colors.shadow,
    };

    Object.entries(spinnerStyles).forEach(([k, v]) =>
      this.renderer.setStyle(spinner, k, v)
    );

    this.renderer.appendChild(container, spinner);
  }

  private createSpinner(container: HTMLElement): void {
    const spinner = this.renderer.createElement('div') as HTMLElement;
    this.renderer.addClass(spinner, 'loader-spinner');

    const sizeMap = { small: 24, medium: 48, large: 64 };
    const size = sizeMap[this.loadingSize()] ?? 48;

    this.renderer.setStyle(spinner, 'width', `${size}px`);
    this.renderer.setStyle(spinner, 'height', `${size}px`);
    this.renderer.setStyle(spinner, 'border', '4px solid rgba(0,0,0,0.1)');
    this.renderer.setStyle(spinner, 'border-top-color', this.loadingColor() || '#1E88E5');
    this.renderer.setStyle(spinner, 'border-radius', '50%');
    this.renderer.setStyle(spinner, 'animation', 'loader-spin 0.8s linear infinite');
    this.renderer.setStyle(spinner, 'will-change', 'transform');
    this.renderer.setStyle(spinner, 'backface-visibility', 'hidden');

    this.renderer.appendChild(container, spinner);
  }

  private createMessageElement(container: HTMLElement, message: string): void {
    const msg = this.renderer.createElement('span') as HTMLElement;
    this.renderer.addClass(msg, 'loader-text');
    this.renderer.setStyle(msg, 'margin-top', '1rem');
    this.renderer.setStyle(msg, 'font-size', '1rem');
    this.renderer.setStyle(msg, 'font-weight', '500');
    this.renderer.setStyle(msg, 'color', '#333');
    this.renderer.setStyle(msg, 'text-align', 'center');
    this.renderer.appendChild(msg, this.renderer.createText(message));
    this.renderer.appendChild(container, msg);
  }

  /**
   * ✅ ULTRA-CLEAR: Get maximum contrast colors with shadows
   */
  private getUltraContrastColors(button: HTMLElement, style: CSSStyleDeclaration): {
    spinner: string;
    track: string;
    shadow: string;
  } {
    // Use custom color if provided
    if (this.loadingColor()) {
      return {
        spinner: this.loadingColor()!,
        track: 'rgba(0, 0, 0, 0.1)',
        shadow: `0 0 8px ${this.loadingColor()}, 0 0 12px ${this.loadingColor()}80`,
      };
    }

    const bg = style.backgroundColor;
    const brightness = this.getColorBrightness(bg);
    
    // ✅ Enhanced contrast logic
    if (brightness < 100) {
      // Very dark background - bright white with glow
      return {
        spinner: '#ffffff',
        track: 'rgba(255, 255, 255, 0.3)',
        shadow: '0 0 8px rgba(255, 255, 255, 0.8), 0 0 12px rgba(255, 255, 255, 0.5)',
      };
    } else if (brightness < 150) {
      // Dark background - white with subtle glow
      return {
        spinner: '#ffffff',
        track: 'rgba(255, 255, 255, 0.25)',
        shadow: '0 0 6px rgba(255, 255, 255, 0.6)',
      };
    } else if (brightness > 200) {
      // Very light background - dark with contrast
      return {
        spinner: '#1a56db',
        track: 'rgba(0, 0, 0, 0.15)',
        shadow: '0 0 6px rgba(26, 86, 219, 0.4)',
      };
    } else {
      // Medium brightness - blue with glow
      return {
        spinner: '#2563eb',
        track: 'rgba(0, 0, 0, 0.12)',
        shadow: '0 0 6px rgba(37, 99, 235, 0.5)',
      };
    }
  }

  /**
   * ✅ FIX: Added null check for color
   */
  private getColorBrightness(color: string): number {
    if (!color || color === 'transparent' || color === 'rgba(0, 0, 0, 0)') {
      return 128; // Default to medium brightness
    }
    
    const rgb = color.match(/\d+/g);
    if (!rgb || rgb.length < 3) return 128;
    const [r, g, b] = rgb.map(Number);
    return (r * 299 + g * 587 + b * 114) / 1000;
  }

  /**
   * ✅ FIXED: Proper cleanup with callback support
   */
  private removeLoadingIndicator(el: HTMLElement, callback?: () => void): void {
    const container = this.loadingContainer;
    if (!container) {
      callback?.();
      return;
    }

    if (this.cleanupTimeoutId) {
      clearTimeout(this.cleanupTimeoutId);
      this.cleanupTimeoutId = null;
    }

    // Restore visibility of original button content
    if (this.elementType === 'button') {
      Array.from(el.childNodes).forEach((child) => {
        if (child !== container && child.nodeType === Node.ELEMENT_NODE) {
          this.renderer.removeStyle(child as HTMLElement, 'visibility');
        }
      });
    }

    if (this.elementType !== 'button') {
      this.renderer.setStyle(container, 'opacity', '0');
      
      this.cleanupTimeoutId = setTimeout(() => {
        if (
          this.loadingContainer === container && 
          container.parentNode === el &&
          !this.isCurrentlyLoading
        ) {
          try {
            this.renderer.removeChild(el, container);
          } catch (e) {
            console.debug('[LoadingDirective] Container already removed', e);
          }
          this.loadingContainer = null;
        }
        this.cleanupTimeoutId = null;
        callback?.();
      }, 150);
    } else {
      // Immediate removal for buttons
      if (container.parentNode === el) {
        try {
          this.renderer.removeChild(el, container);
        } catch (e) {
          console.debug('[LoadingDirective] Container already removed', e);
        }
      }
      this.loadingContainer = null;
      callback?.();
    }
  }

  private cleanup(): void {
    if (this.cleanupTimeoutId) {
      clearTimeout(this.cleanupTimeoutId);
      this.cleanupTimeoutId = null;
    }
    
    if (this.loadingContainer && this.loadingContainer.parentNode) {
      try {
        this.renderer.removeChild(
          this.loadingContainer.parentNode,
          this.loadingContainer
        );
      } catch (e) {
        console.debug('[LoadingDirective] Final cleanup failed', e);
      }
    }
    
    // Restore button content visibility
    if (this.elementType === 'button') {
      const el = this.el.nativeElement as HTMLElement;
      Array.from(el.childNodes).forEach((child) => {
        if (child.nodeType === Node.ELEMENT_NODE) {
          this.renderer.removeStyle(child as HTMLElement, 'visibility');
        }
      });
    }
    
    this.originalState = null;
    this.loadingContainer = null;
    this.isCurrentlyLoading = false;
  }
}
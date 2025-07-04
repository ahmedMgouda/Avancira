import { Directive, ElementRef, HostListener, Input, OnInit, Renderer2 } from '@angular/core';

@Directive({
  selector: '[imageFallback]'
})
export class ImageFallbackDirective implements OnInit {
  @Input('imageFallback') fullName: string = 'U U'; // Default initials

  private imgElement: HTMLImageElement;
  private fallbackDiv: HTMLDivElement;

  constructor(private el: ElementRef, private renderer: Renderer2) {
    this.imgElement = this.el.nativeElement as HTMLImageElement;
    this.fallbackDiv = this.renderer.createElement('div');
  }

  ngOnInit() {
    this.setupFallbackDiv();
    if (!this.imgElement.src) {
      this.showFallback();
    }
  }

  @HostListener('error')
  onError() {
    this.showFallback();
  }

  @HostListener('load')
  onLoad() {
    this.showImage();
  }

  private setupFallbackDiv() {
    const parent = this.imgElement.parentElement;
    if (!parent) return;

    this.renderer.setStyle(this.fallbackDiv, 'display', 'flex');
    this.renderer.setStyle(this.fallbackDiv, 'align-items', 'center');
    this.renderer.setStyle(this.fallbackDiv, 'justify-content', 'center');
    this.renderer.setStyle(this.fallbackDiv, 'background', '#ccc'); // Default background color
    this.renderer.setStyle(this.fallbackDiv, 'color', '#fff');
    this.renderer.setStyle(this.fallbackDiv, 'font-weight', 'bold');
    this.renderer.setStyle(this.fallbackDiv, 'font-size', '1.2rem');
    this.renderer.setStyle(this.fallbackDiv, 'border-radius', '50%');
    this.renderer.setStyle(this.fallbackDiv, 'overflow', 'hidden');
    this.renderer.setStyle(this.fallbackDiv, 'width', `${this.imgElement.width || 100}px`);
    this.renderer.setStyle(this.fallbackDiv, 'height', `${this.imgElement.height || 100}px`);

    const initials = this.getInitials(this.fullName);
    const textNode = this.renderer.createText(initials);
    this.renderer.appendChild(this.fallbackDiv, textNode);

    this.renderer.insertBefore(parent, this.fallbackDiv, this.imgElement.nextSibling);
    this.renderer.setStyle(this.fallbackDiv, 'display', 'none');
  }

  private showFallback() {
    this.renderer.setStyle(this.imgElement, 'display', 'none');
    this.renderer.setStyle(this.fallbackDiv, 'display', 'flex');
  }

  private showImage() {
    this.renderer.setStyle(this.imgElement, 'display', 'block');
    this.renderer.setStyle(this.fallbackDiv, 'display', 'none');
  }

  private getInitials(name: string): string {
    const nameParts = name.trim().split(' ');
    if (nameParts.length === 1) return nameParts[0].charAt(0).toUpperCase();
    return nameParts[0].charAt(0).toUpperCase() + nameParts[1].charAt(0).toUpperCase();
  }
}

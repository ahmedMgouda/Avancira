import { Directive, ElementRef, HostListener, Input, OnInit,Renderer2 } from '@angular/core';

@Directive({
  selector: '[imageFallback]'
})
export class ImageFallbackDirective implements OnInit {
  @Input('imageFallback') fullName: string = 'U U'; // Default initials

  private imgElement: HTMLImageElement;
  private containerDiv: HTMLDivElement | undefined;

  constructor(private el: ElementRef, private renderer: Renderer2) {
    this.imgElement = this.el.nativeElement as HTMLImageElement;
  }

  ngOnInit() {
    if (!this.imgElement.src) {
      this.replaceWithInitials();
    }
  }

  @HostListener('error')
  onError() {
    this.replaceWithInitials();
  }

  private replaceWithInitials() {
    const parent = this.imgElement.parentElement;
    
    // Create initials div
    this.containerDiv = this.renderer.createElement('div');
    this.renderer.setStyle(this.containerDiv, 'display', 'flex');
    this.renderer.setStyle(this.containerDiv, 'align-items', 'center');
    this.renderer.setStyle(this.containerDiv, 'justify-content', 'center');
    this.renderer.setStyle(this.containerDiv, 'background', '#ccc'); // Default background color
    this.renderer.setStyle(this.containerDiv, 'color', '#fff'); // Text color
    this.renderer.setStyle(this.containerDiv, 'font-weight', 'bold');
    this.renderer.setStyle(this.containerDiv, 'font-size', '1.2rem');
    this.renderer.setStyle(this.containerDiv, 'border-radius', '50%');
    this.renderer.setStyle(this.containerDiv, 'overflow', 'hidden');

    // Get initials from name
    const initials = this.getInitials(this.fullName);
    const textNode = this.renderer.createText(initials);
    this.renderer.appendChild(this.containerDiv, textNode);

    // Copy width & height from original image
    this.renderer.setStyle(this.containerDiv, 'width', `${this.imgElement.clientWidth}px`);
    this.renderer.setStyle(this.containerDiv, 'height', `${this.imgElement.clientHeight}px`);

    // Replace image with initials div
    if (parent) {
      this.renderer.removeChild(parent, this.imgElement);
      this.renderer.appendChild(parent, this.containerDiv);
    }
  }

  private getInitials(name: string): string {
    const nameParts = name.trim().split(' ');
    if (nameParts.length === 1) return nameParts[0].charAt(0).toUpperCase();
    return nameParts[0].charAt(0).toUpperCase() + nameParts[1].charAt(0).toUpperCase();
  }
}

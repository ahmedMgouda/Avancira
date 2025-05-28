import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-modal',
  imports: [CommonModule],
  templateUrl: './modal.component.html',
  styleUrl: './modal.component.scss'
})
export class ModalComponent {
  @Input() isOpen = false;
  @Input() size: 'sm'| 'md' | 'lg' = 'md';
  @Input() title = '';
  @Output() onClose = new EventEmitter<void>();

  getModalSizeClass(): string {
    switch (this.size) {
      case 'sm': return 'modal-sm';
      case 'lg': return 'modal-lg';
      default: return '';
    }
  }

  close() {
    this.onClose.emit();
  }
}

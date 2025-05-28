import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

import { ModalComponent } from '../modal/modal.component';

@Component({
  selector: 'app-multi-step-modal',
  imports: [CommonModule, ModalComponent],
  templateUrl: './multi-step-modal.component.html',
  styleUrl: './multi-step-modal.component.scss'
})
export class MultiStepModalComponent {
  @Input() isOpen = false;
  @Input() title = '';
  @Input() step = 1;
  @Input() size: 'sm' | 'md' | 'lg' = 'md';
  @Input() isStepValid!: () => boolean;
  @Input() totalSteps = 1;
  @Input() stepLabels: string[] = [];
  @Output() stepChange = new EventEmitter<number>();
  @Output() onClose = new EventEmitter<void>();
  @Output() onSubmit = new EventEmitter<void>();


  getProgress(): number {
    return (this.step / this.totalSteps) * 100;
  }

  nextStep() {
    if (this.step < this.totalSteps) {
      this.step++;
      this.stepChange.emit(this.step);
    }
  }

  prevStep() {
    if (this.step > 1) {
      this.step--;
      this.stepChange.emit(this.step);
    }
  }

  submit() {
    this.onSubmit.emit();
  }

  close() {
    this.onClose.emit();
  }
}

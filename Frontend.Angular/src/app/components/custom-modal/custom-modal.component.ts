import { Component, Input, Output, EventEmitter, TemplateRef } from '@angular/core';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-custom-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './custom-modal.component.html',
  styleUrls: ['./custom-modal.component.scss'],
})
export class CustomModalComponent {
  @Input() title: string = '';
  @Input() contentTemplate!: TemplateRef<any>;
  @Input() saveButtonLabel: string = 'Save';
  @Input() closeButtonLabel: string = 'Close';

  // Add Confirmation Properties
  @Input() isConfirmation: boolean = false;
  @Input() confirmationMessage: string = '';

  @Output() onSave = new EventEmitter<void>();
  @Output() onClose = new EventEmitter<void>();

  constructor(public activeModal: NgbActiveModal) { }

  save() {
    this.onSave.emit();
    this.activeModal.close();
  }

  close() {
    this.onClose.emit();
    this.activeModal.dismiss();
  }
}

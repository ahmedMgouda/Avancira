import { Injectable, TemplateRef } from '@angular/core';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { CustomModalComponent } from '../components/custom-modal/custom-modal.component';

@Injectable({
  providedIn: 'root',
})
export class ModalService {
  constructor(private modalService: NgbModal) { }

  open(
    title: string,
    content: TemplateRef<any>,
    onSaveCallback?: () => void,
    onCloseCallback?: () => void,
    size: 'sm' | 'md' | 'lg' | 'xl' = 'lg',
    saveLabel = 'Save',
    closeLabel = 'Close') {
    const modalRef = this.modalService.open(CustomModalComponent,
      {
        size: size,
        backdrop: 'static',
        keyboard: false,
        centered: true
      });
    modalRef.componentInstance.title = title;
    modalRef.componentInstance.contentTemplate = content;
    modalRef.componentInstance.saveButtonLabel = saveLabel;
    modalRef.componentInstance.closeButtonLabel = closeLabel;

    if (onSaveCallback) {
      modalRef.componentInstance.onSave.subscribe(onSaveCallback);
    }

    if (onCloseCallback) {
      modalRef.componentInstance.onClose.subscribe(onCloseCallback);
    }
  }

  /** Opens a simple confirmation modal */
  openConfirmation(
    title: string,
    message: string,
    onConfirm: () => void,
    onCancel?: () => void
  ) {
    const modalRef = this.modalService.open(CustomModalComponent, {
      size: 'md',
      backdrop: 'static',
      keyboard: false,
      centered: true
    });

    modalRef.componentInstance.title = title;
    modalRef.componentInstance.isConfirmation = true;
    modalRef.componentInstance.confirmationMessage = message;
    modalRef.componentInstance.saveButtonLabel = 'Confirm';
    modalRef.componentInstance.closeButtonLabel = 'Cancel';

    modalRef.componentInstance.onSave.subscribe(onConfirm);
    if (onCancel) {
      modalRef.componentInstance.onClose.subscribe(onCancel);
    }
  }
}

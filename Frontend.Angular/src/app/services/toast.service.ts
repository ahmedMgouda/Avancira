import { Injectable } from '@angular/core';
import { Toast } from '@syncfusion/ej2-notifications';

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  private toastObj!: Toast;
  private toastContainerId = 'syncfusion-toast-container';

  constructor() {
    this.createToastElement();
  }

  private createToastElement() {
    let toastContainer = document.getElementById(this.toastContainerId);

    // Prevent duplicate container creation
    if (!toastContainer) {
      toastContainer = document.createElement('div');
      toastContainer.id = this.toastContainerId;
      document.body.appendChild(toastContainer);
    }

    this.toastObj = new Toast({
      position: { X: 'Right', Y: 'Top' },
      timeOut: 3000,
      animation: { show: { effect: 'FadeIn' }, hide: { effect: 'FadeOut' } }
    });

    this.toastObj.appendTo(toastContainer);
  }

  showToast(message: string, title: string = '', cssClass: string = 'e-toast-success') {
    if (!this.toastObj) {
      console.error('ToastService: Toast instance is not initialized.');
      return;
    }

    this.toastObj.title = title;
    this.toastObj.content = message;
    this.toastObj.cssClass = cssClass;
    this.toastObj.show();
  }

  showSuccess(message: string) {
    this.showToast(message, '', 'e-toast-success');
  }

  showError(message: string) {
    this.showToast(message, '', 'e-toast-danger');
  }

  showWarning(message: string) {
    this.showToast(message, '', 'e-toast-warning');
  }

  showInfo(message: string) {
    this.showToast(message, '', 'e-toast-info');
  }
}

import { Injectable } from '@angular/core';
import { createSpinner, hideSpinner,showSpinner } from '@syncfusion/ej2-popups';

@Injectable({
  providedIn: 'root',
})
export class SpinnerService {
  private readonly spinnerContainerId = 'globalSpinnerContainer';

  constructor() {
    this.ensureGlobalSpinner();
  }

  /** Ensure a global spinner exists in the DOM */
  private ensureGlobalSpinner(): void {
    if (!document.getElementById(this.spinnerContainerId)) {
      const container = document.createElement('div');
      container.id = this.spinnerContainerId;
      container.style.position = 'fixed';
      container.style.top = '0';
      container.style.left = '0';
      container.style.width = '100%';
      container.style.height = '100%';
      container.style.zIndex = '9999';
      container.style.background = 'rgba(0, 0, 0, 0.3)';
      container.style.display = 'none'; // Hidden initially
      document.body.appendChild(container);

      this.initSpinner();
    }
  }

  /** Initialize the Syncfusion spinner */
  private initSpinner(): void {
    const target = document.getElementById(this.spinnerContainerId);
    if (target) {
      createSpinner({
        target,
        cssClass: 'e-spin-overlay',
        label: 'Loading...',
      });
    }
  }

  /** Show the spinner */
  show(): void {
    const target = document.getElementById(this.spinnerContainerId);
    if (target) {
      target.style.display = 'flex';
      showSpinner(target);
    }
  }

  /** Hide the spinner */
  hide(): void {
    const target = document.getElementById(this.spinnerContainerId);
    if (target) {
      hideSpinner(target);
      target.style.display = 'none';
    }
  }
}

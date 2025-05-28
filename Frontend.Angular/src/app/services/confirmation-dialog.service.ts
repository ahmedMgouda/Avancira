import { Injectable } from '@angular/core';
import { DialogComponent } from '@syncfusion/ej2-angular-popups';

@Injectable({
  providedIn: 'root',
})
export class ConfirmationDialogService {
  private dialog!: DialogComponent;
  private resolveFunction!: (result: boolean) => void;

  // Initialize the dialog reference
  initialize(dialog: DialogComponent) {
    this.dialog = dialog;
  }

  // Open confirmation dialog and return user's choice as a Promise
  confirm(message: string, title: string = 'Confirmation', confirmText: string = 'Yes', cancelText: string = 'No'): Promise<boolean> {
    return new Promise<boolean>((resolve) => {
      this.resolveFunction = resolve;

      this.dialog.header = title;
      this.dialog.content = message;
      this.dialog.buttons = [
        {
          click: () => this.close(true),
          buttonModel: { content: confirmText, isPrimary: true },
        },
        {
          click: () => this.close(false),
          buttonModel: { content: cancelText },
        },
      ];
      this.dialog.show();
    });
  }

  // Close the dialog and resolve the user's choice
  private close(result: boolean) {
    this.dialog.hide();
    this.resolveFunction(result);
  }
}

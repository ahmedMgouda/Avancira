import { AfterViewInit, Component, ViewChild } from '@angular/core';
import { ButtonModule } from '@syncfusion/ej2-angular-buttons';
import {DialogComponent,DialogModule } from '@syncfusion/ej2-angular-popups';

import { ConfirmationDialogService } from '../../services/confirmation-dialog.service';

@Component({
  selector: 'app-confirmation-dialog',
  imports: [DialogModule, ButtonModule],
  templateUrl: './confirmation-dialog.component.html',
  styleUrl: './confirmation-dialog.component.scss'
})
export class ConfirmationDialogComponent implements AfterViewInit {
  @ViewChild('confirmationDialog') public confirmationDialog!: DialogComponent;

  constructor(private dialogService: ConfirmationDialogService) {}

  ngAfterViewInit() {
    this.dialogService.initialize(this.confirmationDialog);
  }
}

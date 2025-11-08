/**
 * Confirm Dialog Component
 * Reusable confirmation dialog with customizable buttons and styling
 */

import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule,MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';

import { ConfirmDialogConfig, ConfirmDialogResult } from '../models/dialog.models';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, MatIconModule],
  template: `
    <div class="dialog-container">
      @if (data.icon) {
        <div class="dialog-icon" [style.color]="data.icon.color">
          <mat-icon>{{ data.icon.name }}</mat-icon>
        </div>
      }

      @if (data.title) {
        <h2 mat-dialog-title class="dialog-title">{{ data.title }}</h2>
      }

      <mat-dialog-content class="dialog-content">
        <p class="dialog-message">{{ data.message }}</p>
      </mat-dialog-content>

      <mat-dialog-actions align="end" class="dialog-actions">
        <button
          mat-button
          (click)="onCancel()"
          type="button"
          cdkFocusInitial>
          {{ data.cancelText }}
        </button>
        <button
          mat-raised-button
          [color]="data.confirmColor || 'primary'"
          (click)="onConfirm()"
          type="button">
          {{ data.confirmText }}
        </button>
      </mat-dialog-actions>
    </div>
  `,
  styles: [`
    .dialog-container {
      padding: 0;
    }

    .dialog-icon {
      display: flex;
      justify-content: center;
      margin-bottom: 16px;
      
      mat-icon {
        font-size: 48px;
        width: 48px;
        height: 48px;
      }
    }

    .dialog-title {
      text-align: center;
      margin: 0 0 16px 0;
      font-size: 20px;
      font-weight: 500;
    }

    .dialog-content {
      padding: 0 24px;
      margin: 0;
      max-height: 60vh;
      overflow-y: auto;
    }

    .dialog-message {
      margin: 0;
      color: rgba(0, 0, 0, 0.87);
      font-size: 14px;
      line-height: 1.5;
      text-align: center;
    }

    .dialog-actions {
      padding: 16px 24px;
      margin: 0;
      min-height: auto;
    }

    :host-context(.dark-theme) .dialog-message {
      color: rgba(255, 255, 255, 0.87);
    }
  `],
})
export class ConfirmDialogComponent {
  readonly data = inject<ConfirmDialogConfig>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<ConfirmDialogComponent, ConfirmDialogResult>);

  onConfirm(): void {
    this.dialogRef.close({ confirmed: true });
  }

  onCancel(): void {
    this.dialogRef.close({ confirmed: false });
  }
}
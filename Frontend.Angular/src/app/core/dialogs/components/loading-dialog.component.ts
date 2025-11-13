import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { LoadingDialogConfig } from '../models/dialog.models';

@Component({
  selector: 'app-loading-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatProgressSpinnerModule,
    MatProgressBarModule,
  ],
  template: `
    <div class="loading-dialog">
      <div class="loading-content">
        @if (!data.showProgress) {
          <mat-spinner [diameter]="50"></mat-spinner>
        }

        <p class="loading-message">{{ message }}</p>

        @if (data.showProgress) {
          <mat-progress-bar
            [mode]="data.indeterminate ? 'indeterminate' : 'determinate'"
            [value]="progress"
          ></mat-progress-bar>

          @if (!data.indeterminate) {
            <span class="progress-text">{{ progress }}%</span>
          }
        }
      </div>
    </div>
  `,
  styles: [
    `
      .loading-dialog {
        padding: 24px;
        min-width: 250px;
      }

      .loading-content {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 16px;
      }

      .loading-message {
        margin: 0;
        text-align: center;
        color: rgba(0, 0, 0, 0.87);
        font-size: 14px;
      }

      mat-progress-bar {
        width: 100%;
      }

      .progress-text {
        font-size: 12px;
        color: rgba(0, 0, 0, 0.6);
      }

      :host-context(.dark-theme) .loading-message {
        color: rgba(255, 255, 255, 0.87);
      }

      :host-context(.dark-theme) .progress-text {
        color: rgba(255, 255, 255, 0.6);
      }
    `,
  ],
})
export class LoadingDialogComponent {
  readonly data = inject<LoadingDialogConfig>(MAT_DIALOG_DATA);

  message = this.data.message;
  progress = this.data.progress ?? 0;
}

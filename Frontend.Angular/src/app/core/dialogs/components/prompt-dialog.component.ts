/**
 * Prompt Dialog Component
 * Dialog for collecting user input with validation support
 * 
 * ENHANCEMENTS:
 * ✅ Number input min/max/step validation
 * ✅ Helper text support
 * ✅ Better error messages
 */

import { Component, inject, OnInit } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule,MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';

import { PromptDialogConfig, PromptDialogResult } from '../models/dialog.models';

@Component({
  selector: 'app-prompt-dialog',
  standalone: true,
  imports: [
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    ReactiveFormsModule,
  ],
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

        <form (ngSubmit)="onSubmit()" class="dialog-form">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>{{ data.placeholder || 'Enter value' }}</mat-label>
            @if (data.inputType === 'textarea') {
              <textarea
                matInput
                [formControl]="inputControl"
                [placeholder]="data.placeholder || ''"
                rows="4"
                cdkFocusInitial></textarea>
            } @else {
              <input
                matInput
                [type]="data.inputType || 'text'"
                [formControl]="inputControl"
                [placeholder]="data.placeholder || ''"
                [min]="data.min"
                [max]="data.max"
                [step]="data.step"
                cdkFocusInitial />
            }
            
            <!-- Helper text -->
            @if (data.helperText) {
              <mat-hint>{{ data.helperText }}</mat-hint>
            }
            
            <!-- Error messages -->
            @if (inputControl.hasError('required')) {
              <mat-error>This field is required</mat-error>
            }
            @if (inputControl.hasError('minlength')) {
              <mat-error>
                Minimum length is {{ data.minLength }} characters
              </mat-error>
            }
            @if (inputControl.hasError('maxlength')) {
              <mat-error>
                Maximum length is {{ data.maxLength }} characters
              </mat-error>
            }
            @if (inputControl.hasError('min')) {
              <mat-error>
                Minimum value is {{ data.min }}
              </mat-error>
            }
            @if (inputControl.hasError('max')) {
              <mat-error>
                Maximum value is {{ data.max }}
              </mat-error>
            }
            @if (inputControl.hasError('pattern')) {
              <mat-error>Invalid format</mat-error>
            }
            @if (inputControl.hasError('email')) {
              <mat-error>Invalid email address</mat-error>
            }
          </mat-form-field>
        </form>
      </mat-dialog-content>

      <mat-dialog-actions align="end" class="dialog-actions">
        <button
          mat-button
          (click)="onCancel()"
          type="button">
          {{ data.cancelText }}
        </button>
        <button
          mat-raised-button
          color="primary"
          (click)="onSubmit()"
          type="button"
          [disabled]="inputControl.invalid">
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
      margin: 0 0 16px 0;
      color: rgba(0, 0, 0, 0.87);
      font-size: 14px;
      line-height: 1.5;
      text-align: center;
    }

    .dialog-form {
      margin-top: 16px;
    }

    .full-width {
      width: 100%;
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
export class PromptDialogComponent implements OnInit {
  readonly data = inject<PromptDialogConfig>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<PromptDialogComponent, PromptDialogResult>);

  inputControl = new FormControl('');

  ngOnInit(): void {
    // Set default value
    if (this.data.defaultValue) {
      this.inputControl.setValue(this.data.defaultValue);
    }

    // Setup validators
    const validators = [];

    if (this.data.required) {
      validators.push(Validators.required);
    }

    if (this.data.minLength) {
      validators.push(Validators.minLength(this.data.minLength));
    }

    if (this.data.maxLength) {
      validators.push(Validators.maxLength(this.data.maxLength));
    }

    // NEW: Number validation
    if (this.data.min !== undefined) {
      validators.push(Validators.min(this.data.min));
    }

    if (this.data.max !== undefined) {
      validators.push(Validators.max(this.data.max));
    }

    if (this.data.pattern) {
      validators.push(Validators.pattern(this.data.pattern));
    }

    if (this.data.inputType === 'email') {
      validators.push(Validators.email);
    }

    if (validators.length > 0) {
      this.inputControl.setValidators(validators);
      this.inputControl.updateValueAndValidity();
    }
  }

  onSubmit(): void {
    if (this.inputControl.valid) {
      this.dialogRef.close({
        submitted: true,
        value: this.inputControl.value || '',
      });
    }
  }

  onCancel(): void {
    this.dialogRef.close({ submitted: false });
  }
}

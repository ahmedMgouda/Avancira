// shared/components/file-upload/file-upload.component.ts
/**
 * File Upload Component - FIXED & ALIGNED
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * IMPROVEMENTS:
 *   ✅ Uses CleanableService for automatic cleanup
 *   ✅ Proper subscription management
 *   ✅ No memory leaks
 *   ✅ Better error handling
 */

import { CommonModule } from '@angular/common';
import { Component, EventEmitter, inject, Input, OnInit, Output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';

import { FileUploadService } from '../services/file-upload.service';

import { CleanableService } from '../../utils/cleanup-manager.utility';
import {
  FileMetadata,
  FileType,
  FileUploadConfig,
} from '../models/file-upload.models';

@Component({
  selector: 'app-file-upload',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatTooltipModule,
  ],
  template: `
    <div class="file-upload-container">
      <!-- Drop Zone -->
      <div
        class="drop-zone"
        [class.drag-over]="isDragOver"
        [class.disabled]="disabled || isMaxFilesReached"
        (dragover)="onDragOver($event)"
        (dragleave)="onDragLeave($event)"
        (drop)="onDrop($event)"
        (click)="fileInput.click()">
        <input
          #fileInput
          type="file"
          class="file-input"
          [accept]="acceptTypes"
          [multiple]="config.multiple"
          [disabled]="disabled || isMaxFilesReached"
          (change)="onFileSelected($event)" />

        <div class="drop-zone-content">
          <mat-icon class="upload-icon">cloud_upload</mat-icon>
          <p class="drop-zone-text">
            {{ isDragOver ? 'Drop files here' : 'Drag & drop files here or click to browse' }}
          </p>
          <p class="drop-zone-hint">
            @if (config.allowedExtensions && config.allowedExtensions.length > 0) {
              <span>Allowed: {{ config.allowedExtensions.join(', ') }}</span>
              <br />
            }
            @if (config.maxSize) {
              <span>Max size: {{ fileUploadService.formatFileSize(config.maxSize) }}</span>
              <br />
            }
            @if (config.maxFiles) {
              <span>Max files: {{ config.maxFiles }}</span>
            }
          </p>
        </div>
      </div>

      <!-- Validation Errors -->
      @if (validationErrors.length > 0) {
        <div class="error-messages">
          @for (error of validationErrors; track error) {
            <div class="error-message">
              <mat-icon>error</mat-icon>
              <span>{{ error }}</span>
            </div>
          }
        </div>
      }

      <!-- File List -->
      @if (files.length > 0) {
        <div class="file-list">
          @for (file of files; track file.id) {
            <div class="file-item" [class.uploading]="file.uploading" [class.error]="file.error">
              <!-- Preview -->
              <div class="file-preview">
                @if (file.preview) {
                  <img [src]="file.preview" [alt]="file.name" class="preview-image" />
                } @else {
                  <mat-icon class="file-icon">{{ getFileIcon(file.type) }}</mat-icon>
                }
              </div>

              <!-- File Info -->
              <div class="file-info">
                <div class="file-name" [matTooltip]="file.name">{{ file.name }}</div>
                <div class="file-size">{{ fileUploadService.formatFileSize(file.size) }}</div>
                
                @if (file.uploading) {
                  <mat-progress-bar
                    mode="determinate"
                    [value]="file.progress || 0"
                    class="upload-progress">
                  </mat-progress-bar>
                  <div class="progress-text">{{ file.progress }}%</div>
                } @else if (file.error) {
                  <div class="error-text">{{ file.error }}</div>
                } @else if (file.uploaded) {
                  <div class="success-text">
                    <mat-icon class="success-icon">check_circle</mat-icon>
                    Uploaded
                  </div>
                }
              </div>

              <!-- Actions -->
              <div class="file-actions">
                @if (!file.uploading && !disabled) {
                  <button
                    mat-icon-button
                    (click)="removeFile(file.id)"
                    matTooltip="Remove file"
                    class="remove-btn">
                    <mat-icon>close</mat-icon>
                  </button>
                }
              </div>
            </div>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .file-upload-container {
      width: 100%;
    }

    .drop-zone {
      border: 2px dashed #ccc;
      border-radius: 8px;
      padding: 40px;
      text-align: center;
      cursor: pointer;
      transition: all 0.3s ease;
      background-color: #fafafa;
    }

    .drop-zone:hover:not(.disabled) {
      border-color: #2196f3;
      background-color: #f0f8ff;
    }

    .drop-zone.drag-over {
      border-color: #4caf50;
      background-color: #e8f5e9;
    }

    .drop-zone.disabled {
      opacity: 0.5;
      cursor: not-allowed;
      pointer-events: none;
    }

    .file-input {
      display: none;
    }

    .drop-zone-content {
      pointer-events: none;
    }

    .upload-icon {
      font-size: 64px;
      width: 64px;
      height: 64px;
      color: #757575;
      margin-bottom: 16px;
    }

    .drop-zone-text {
      font-size: 16px;
      color: #424242;
      margin: 0 0 8px 0;
    }

    .drop-zone-hint {
      font-size: 12px;
      color: #757575;
      margin: 0;
      line-height: 1.6;
    }

    .error-messages {
      margin-top: 16px;
    }

    .error-message {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 12px;
      background-color: #ffebee;
      border-radius: 4px;
      color: #c62828;
      margin-bottom: 8px;

      mat-icon {
        font-size: 20px;
        width: 20px;
        height: 20px;
      }
    }

    .file-list {
      margin-top: 24px;
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .file-item {
      display: flex;
      align-items: center;
      gap: 16px;
      padding: 12px;
      border: 1px solid #e0e0e0;
      border-radius: 8px;
      background-color: #fff;
      transition: all 0.3s ease;
    }

    .file-item:hover {
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    }

    .file-item.uploading {
      background-color: #f5f5f5;
    }

    .file-item.error {
      border-color: #f44336;
      background-color: #ffebee;
    }

    .file-preview {
      flex-shrink: 0;
      width: 60px;
      height: 60px;
      display: flex;
      align-items: center;
      justify-content: center;
      background-color: #f5f5f5;
      border-radius: 4px;
      overflow: hidden;
    }

    .preview-image {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }

    .file-icon {
      font-size: 36px;
      width: 36px;
      height: 36px;
      color: #757575;
    }

    .file-info {
      flex: 1;
      min-width: 0;
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .file-name {
      font-weight: 500;
      color: #212121;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .file-size {
      font-size: 12px;
      color: #757575;
    }

    .upload-progress {
      margin-top: 8px;
      height: 4px;
      border-radius: 2px;
    }

    .progress-text {
      font-size: 12px;
      color: #2196f3;
      margin-top: 4px;
    }

    .error-text {
      font-size: 12px;
      color: #f44336;
      margin-top: 4px;
    }

    .success-text {
      display: flex;
      align-items: center;
      gap: 4px;
      font-size: 12px;
      color: #4caf50;
      margin-top: 4px;
    }

    .success-icon {
      font-size: 16px;
      width: 16px;
      height: 16px;
    }

    .file-actions {
      flex-shrink: 0;
    }

    .remove-btn {
      color: #757575;

      &:hover {
        color: #f44336;
      }
    }

    :host-context(.dark-theme) {
      .drop-zone {
        background-color: #303030;
        border-color: #424242;
      }

      .drop-zone:hover:not(.disabled) {
        background-color: #424242;
      }

      .file-item {
        background-color: #424242;
        border-color: #616161;
      }

      .file-preview {
        background-color: #303030;
      }
    }
  `],
})
export class FileUploadComponent extends CleanableService implements OnInit {
  @Input() config: FileUploadConfig = {};
  @Input() fileType: FileType = FileType.Image;
  @Input() disabled = false;
  @Output() filesChanged = new EventEmitter<FileMetadata[]>();
  @Output() filesValidated = new EventEmitter<boolean>();

  fileUploadService = inject(FileUploadService);

  files: FileMetadata[] = [];
  validationErrors: string[] = [];
  isDragOver = false;
  acceptTypes = '';

  constructor() {
    super(); // ✅ Initialize CleanupManager
  }

  ngOnInit(): void {
    // Merge default config with input config
    const defaultConfig = this.fileUploadService.getDefaultConfig(this.fileType);
    this.config = { ...defaultConfig, ...this.config };

    // Build accept attribute
    if (this.config.allowedExtensions) {
      this.acceptTypes = this.config.allowedExtensions.join(',');
    }
  }

  // ✅ Cleanup hook from CleanableService
  protected override onCleanup(): void {
    // Clear all files and revoke blob URLs
    this.fileUploadService.clearFiles(this.files);
    this.files = [];
  }

  get isMaxFilesReached(): boolean {
    return !!this.config.maxFiles && this.files.length >= this.config.maxFiles;
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    if (!this.disabled && !this.isMaxFilesReached) {
      this.isDragOver = true;
    }
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;
  }

  async onDrop(event: DragEvent): Promise<void> {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;

    if (this.disabled || this.isMaxFilesReached) return;

    const files = event.dataTransfer?.files;
    if (files) {
      await this.handleFiles(files);
    }
  }

  async onFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    if (input.files) {
      await this.handleFiles(input.files);
      input.value = ''; // Reset input
    }
  }

  private async handleFiles(fileList: FileList): Promise<void> {
    const filesArray = Array.from(fileList);
    
    // Check if adding these files would exceed max files
    const remainingSlots = this.config.maxFiles
      ? this.config.maxFiles - this.files.length
      : Infinity;
    const filesToAdd = filesArray.slice(0, remainingSlots);

    // Validate files
    const validation = this.fileUploadService.validateFiles(filesToAdd, this.config);
    this.validationErrors = validation.errors;
    this.filesValidated.emit(validation.valid);

    if (!validation.valid) {
      return;
    }

    // Process files
    const newFiles = await this.fileUploadService.processFiles(filesToAdd, this.config);
    this.files = [...this.files, ...newFiles];
    this.filesChanged.emit(this.files);
  }

  removeFile(fileId: string): void {
    this.files = this.fileUploadService.removeFile(this.files, fileId);
    this.validationErrors = [];
    this.filesChanged.emit(this.files);
    this.filesValidated.emit(true);
  }

  getFileIcon(mimeType: string): string {
    if (mimeType.startsWith('image/')) return 'image';
    if (mimeType.startsWith('video/')) return 'video_file';
    if (mimeType.startsWith('audio/')) return 'audio_file';
    if (mimeType.includes('pdf')) return 'picture_as_pdf';
    if (mimeType.includes('word')) return 'description';
    if (mimeType.includes('excel') || mimeType.includes('spreadsheet')) return 'table_chart';
    return 'insert_drive_file';
  }
}
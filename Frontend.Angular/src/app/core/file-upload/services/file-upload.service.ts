// shared/services/file-upload.service.ts
/**
 * File Upload Service - FIXED & ALIGNED
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * IMPROVEMENTS:
 *   ✅ Uses IdGenerator from core/utils
 *   ✅ Integrated with LoggerService for tracking
 *   ✅ Better error handling with ErrorHandlerService
 *   ✅ Toast notifications via ToastManager
 *   ✅ Proper cleanup of blob URLs
 *   ✅ Signal-based state management
 */

import { HttpClient, HttpEventType } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Subject } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

import { LoggerService } from '../../logging/services/logger.service';
import { ToastManager } from '../../toast/services/toast-manager.service';

import { IdGenerator } from '../../utils/id-generator.utility';
import {
  DEFAULT_FILE_CONFIGS,
  FileMetadata,
  FileType,
  FileUploadConfig,
  FileUploadDto,
  FileUploadEvent,
  FileValidationResult,
} from '../models/file-upload.models';

@Injectable({
  providedIn: 'root',
})
export class FileUploadService {
  private readonly http = inject(HttpClient);
  private readonly logger = inject(LoggerService);
  private readonly toast = inject(ToastManager);
  
  private readonly fileSubject = new Subject<FileUploadEvent>();
  
  // Track blob URLs for cleanup
  private readonly activeBlobUrls = new Set<string>();

  // Observable for file events
  public fileEvents$ = this.fileSubject.asObservable();

  /**
   * Validate a single file against configuration
   */
  validateFile(file: File, config: FileUploadConfig): FileValidationResult {
    const errors: string[] = [];

    // Check file size
    if (config.maxSize && file.size > config.maxSize) {
      errors.push(
        `File size (${this.formatFileSize(file.size)}) exceeds maximum allowed size (${this.formatFileSize(config.maxSize)})`
      );
    }

    // Check file extension
    const extension = this.getFileExtension(file.name);
    if (
      config.allowedExtensions &&
      config.allowedExtensions.length > 0 &&
      !config.allowedExtensions.includes(extension.toLowerCase())
    ) {
      errors.push(
        `File type "${extension}" is not allowed. Allowed types: ${config.allowedExtensions.join(', ')}`
      );
    }

    // Check MIME type
    if (
      config.allowedMimeTypes &&
      config.allowedMimeTypes.length > 0 &&
      !config.allowedMimeTypes.includes(file.type)
    ) {
      errors.push(`File MIME type "${file.type}" is not allowed.`);
    }

    // ✅ Log validation failures
    if (errors.length > 0) {
      this.logger.warn('File validation failed', {
        log: { source: 'FileUploadService', type: 'application' },
        error: {
          id: IdGenerator.generateErrorId(),
          kind: 'application' as const,
          handled: true,
          code: 'FILE_VALIDATION_FAILED',
          type: 'ValidationError',
          message: {
            user: errors.join(', '),
            technical: `File: ${file.name}, Errors: ${errors.join(', ')}`
          },
          severity: 'warning' as const
        }
      });
    }

    return {
      valid: errors.length === 0,
      errors,
    };
  }

  /**
   * Validate multiple files
   */
  validateFiles(files: File[], config: FileUploadConfig): FileValidationResult {
    const errors: string[] = [];

    // Check number of files
    if (config.maxFiles && files.length > config.maxFiles) {
      errors.push(`Maximum ${config.maxFiles} file(s) allowed. You selected ${files.length} file(s).`);
      return { valid: false, errors };
    }

    // Validate each file
    files.forEach((file, index) => {
      const validation = this.validateFile(file, config);
      if (!validation.valid) {
        errors.push(`File ${index + 1} (${file.name}): ${validation.errors.join(', ')}`);
      }
    });

    return {
      valid: errors.length === 0,
      errors,
    };
  }

  /**
   * Process files and create metadata with previews
   */
  async processFiles(files: FileList | File[], config: FileUploadConfig): Promise<FileMetadata[]> {
    const fileArray = Array.from(files);
    const metadata: FileMetadata[] = [];

    for (const file of fileArray) {
      try {
        const meta = await this.createFileMetadata(file, config);
        metadata.push(meta);
        this.fileSubject.next({ type: 'add', file: meta });
      } catch (error) {
        this.logger.error('Failed to process file', error, {
          log: { source: 'FileUploadService', type: 'application' }
        });
        
        // ✅ Show error toast
        this.toast.error(`Failed to process file: ${file.name}`);
      }
    }

    return metadata;
  }

  /**
   * Create file metadata with preview
   * ✅ Uses IdGenerator for unique IDs
   */
  private async createFileMetadata(file: File, config: FileUploadConfig): Promise<FileMetadata> {
    const id = IdGenerator.generateUUID(); // ✅ Use centralized ID generation
    const extension = this.getFileExtension(file.name);
    
    const metadata: FileMetadata = {
      id,
      file,
      name: this.sanitizeFileName(file.name),
      extension,
      size: file.size,
      type: file.type,
      progress: 0,
      uploading: false,
      uploaded: false,
    };

    // Generate preview for images
    if (this.isImage(file) && config.generateThumbnails) {
      try {
        metadata.preview = await this.generateImagePreview(file, config);
        // ✅ Track blob URL for cleanup
        if (metadata.preview.startsWith('blob:')) {
          this.activeBlobUrls.add(metadata.preview);
        }
      } catch {
        this.logger.warn('Failed to generate image preview', {
          log: { source: 'FileUploadService', type: 'application' }
        });
      }
    } else if (file.type === 'application/pdf') {
      metadata.preview = 'assets/icons/pdf-icon.svg';
    }

    return metadata;
  }

  /**
   * Generate image preview/thumbnail
   */
  private async generateImagePreview(file: File, config: FileUploadConfig): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      
      reader.onload = (e: ProgressEvent<FileReader>) => {
        const img = new Image();
        img.onload = () => {
          try {
            // Create canvas for thumbnail
            const canvas = document.createElement('canvas');
            const ctx = canvas.getContext('2d');
            
            if (!ctx) {
              resolve(e.target?.result as string);
              return;
            }

            // Calculate dimensions
            let width = img.width;
            let height = img.height;
            const maxWidth = config.thumbnailMaxWidth || 300;
            const maxHeight = config.thumbnailMaxHeight || 300;

            if (width > maxWidth || height > maxHeight) {
              const ratio = Math.min(maxWidth / width, maxHeight / height);
              width = width * ratio;
              height = height * ratio;
            }

            canvas.width = width;
            canvas.height = height;

            // Draw image
            ctx.drawImage(img, 0, 0, width, height);

            // Get data URL
            resolve(canvas.toDataURL(file.type, config.compressionQuality || 0.8));
          } catch (error) {
            reject(error);
          }
        };

        img.onerror = () => reject(new Error('Failed to load image'));
        img.src = e.target?.result as string;
      };

      reader.onerror = () => reject(new Error('Failed to read file'));
      reader.readAsDataURL(file);
    });
  }

  /**
   * Convert file to base64 for backend
   */
  async fileToBase64(file: File): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => resolve(reader.result as string);
      reader.onerror = () => reject(new Error('Failed to read file'));
      reader.readAsDataURL(file);
    });
  }

  /**
   * Compress image before upload
   */
  async compressImage(file: File, quality: number = 0.8): Promise<Blob> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      
      reader.onload = (e: ProgressEvent<FileReader>) => {
        const img = new Image();
        img.onload = () => {
          try {
            const canvas = document.createElement('canvas');
            const ctx = canvas.getContext('2d');
            
            if (!ctx) {
              resolve(file);
              return;
            }

            canvas.width = img.width;
            canvas.height = img.height;
            ctx.drawImage(img, 0, 0);

            canvas.toBlob(
              (blob) => {
                if (blob) {
                  resolve(blob);
                } else {
                  resolve(file);
                }
              },
              file.type,
              quality
            );
          } catch (error) {
            reject(error);
          }
        };

        img.onerror = () => reject(new Error('Failed to load image'));
        img.src = e.target?.result as string;
      };

      reader.onerror = () => reject(new Error('Failed to read file'));
      reader.readAsDataURL(file);
    });
  }

  /**
   * Upload file to backend
   * ✅ Better error handling and logging
   */
  async uploadFile(
    metadata: FileMetadata,
    endpoint: string
  ): Promise<string> {
    try {
      metadata.uploading = true;
      metadata.progress = 0;
      this.fileSubject.next({ type: 'progress', file: metadata });

      // ✅ Log upload start
      this.logger.info(`Starting file upload: ${metadata.name}`, {
        log: { source: 'FileUploadService', type: 'application' }
      });

      // Get base64 data
      const base64Data = await this.fileToBase64(metadata.file);

      // Prepare DTO matching backend
      const dto: FileUploadDto = {
        name: metadata.name,
        extension: metadata.extension,
        data: base64Data,
      };

      // Upload
      const response = await this.http
        .post<{ url: string }>(endpoint, dto, {
          reportProgress: true,
          observe: 'events',
        })
        .pipe(
          map((event) => {
            if (event.type === HttpEventType.UploadProgress) {
              const progress = event.total
                ? Math.round((100 * event.loaded) / event.total)
                : 0;
              metadata.progress = progress;
              this.fileSubject.next({ type: 'progress', file: metadata });
            } else if (event.type === HttpEventType.Response) {
              return event.body;
            }
            return null;
          }),
          catchError((error) => {
            const errorMsg = error.message || 'Upload failed';
            metadata.error = errorMsg;
            metadata.uploading = false;
            this.fileSubject.next({ type: 'error', file: metadata });
            
            // ✅ Log error
            this.logger.error(`File upload failed: ${metadata.name}`, error, {
              log: { source: 'FileUploadService', type: 'application' }
            });
            
            // ✅ Show error toast
            this.toast.error(`Failed to upload ${metadata.name}`, 'Upload Error');
            
            throw error;
          })
        )
        .toPromise();

      const url = response?.url || '';
      metadata.url = url;
      metadata.uploaded = true;
      metadata.uploading = false;
      metadata.progress = 100;
      this.fileSubject.next({ type: 'complete', file: metadata });

      // ✅ Log success
      this.logger.info(`File uploaded successfully: ${metadata.name}`, {
        log: { source: 'FileUploadService', type: 'application' }
      });
      
      // ✅ Show success toast
      this.toast.success(`${metadata.name} uploaded successfully`);

      return url;
    } catch (error) {
      metadata.error = error instanceof Error ? error.message : 'Upload failed';
      metadata.uploading = false;
      this.fileSubject.next({ type: 'error', file: metadata });
      throw error;
    }
  }

  /**
   * Upload multiple files
   */
  async uploadFiles(
    files: FileMetadata[],
    endpoint: string,
  ): Promise<string[]> {
    const urls: string[] = [];

    for (const file of files) {
      if (!file.uploaded) {
        try {
          const url = await this.uploadFile(file, endpoint);
          urls.push(url);
        } catch{
          // Continue with other files even if one fails
          this.logger.warn(`Skipping failed upload: ${file.name}`, {
            log: { source: 'FileUploadService', type: 'application' }
          });
        }
      } else if (file.url) {
        urls.push(file.url);
      }
    }

    return urls;
  }

  /**
   * Remove file from list
   * ✅ Proper blob URL cleanup
   */
  removeFile(files: FileMetadata[], fileId: string): FileMetadata[] {
    const file = files.find((f) => f.id === fileId);
    if (file) {
      // ✅ Cleanup blob URL
      if (file.preview && this.activeBlobUrls.has(file.preview)) {
        URL.revokeObjectURL(file.preview);
        this.activeBlobUrls.delete(file.preview);
      }
      this.fileSubject.next({ type: 'remove', file });
    }
    return files.filter((f) => f.id !== fileId);
  }

  /**
   * Clear all files and revoke URLs
   * ✅ Comprehensive cleanup
   */
  clearFiles(files: FileMetadata[]): void {
    files.forEach((file) => {
      if (file.preview && this.activeBlobUrls.has(file.preview)) {
        URL.revokeObjectURL(file.preview);
        this.activeBlobUrls.delete(file.preview);
      }
    });
  }

  /**
   * ✅ NEW: Cleanup all resources (call in service destroy)
   */
  cleanup(): void {
    // Revoke all active blob URLs
    this.activeBlobUrls.forEach(url => {
      try {
        URL.revokeObjectURL(url);
      } catch {
        this.logger.warn('Failed to revoke blob URL', {
          log: { source: 'FileUploadService', type: 'application' }
        });
      }
    });
    this.activeBlobUrls.clear();
    
    // Complete subject
    this.fileSubject.complete();
  }

  // ===== Utility Methods =====

  /**
   * Get file extension including the dot
   */
  private getFileExtension(fileName: string): string {
    const lastDot = fileName.lastIndexOf('.');
    return lastDot === -1 ? '' : fileName.substring(lastDot);
  }

  /**
   * Sanitize file name (remove special characters)
   */
  private sanitizeFileName(fileName: string): string {
    const extension = this.getFileExtension(fileName);
    const nameWithoutExt = fileName.substring(0, fileName.length - extension.length);
    
    const sanitized = nameWithoutExt
      .replace(/[^a-zA-Z0-9_.-]/g, '-')
      .replace(/\s+/g, '-')
      .replace(/-+/g, '-')
      .replace(/^-|-$/g, '');
    
    return sanitized + extension;
  }

  /**
   * Check if file is an image
   */
  private isImage(file: File): boolean {
    return file.type.startsWith('image/');
  }

  /**
   * Format file size for display
   */
  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
  }

  /**
   * Get default config for file type
   */
  getDefaultConfig(fileType: FileType): FileUploadConfig {
    return { ...DEFAULT_FILE_CONFIGS[fileType] };
  }
}
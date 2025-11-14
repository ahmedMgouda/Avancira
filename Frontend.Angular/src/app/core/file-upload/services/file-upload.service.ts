import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { HttpClient, HttpEventType } from '@angular/common/http';
import { inject, Injectable, PLATFORM_ID } from '@angular/core';
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
  FileUploadResult,
} from '../models/file-upload.models';

/**
 * ═══════════════════════════════════════════════════════════════════════
 * FILE UPLOAD SERVICE - FIXED
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * FIXES:
 * ✅ Added platform checks for all DOM API usage
 * ✅ SSR-safe with proper guards
 * ✅ uploadFiles() now returns detailed results (success + failures)
 * ✅ Callers can retry failed uploads
 * ✅ No silent error swallowing
 */

@Injectable({ providedIn: 'root' })
export class FileUploadService {
  private readonly http = inject(HttpClient);
  private readonly logger = inject(LoggerService);
  private readonly toast = inject(ToastManager);
  private readonly document = inject(DOCUMENT);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);
  
  private readonly fileSubject = new Subject<FileUploadEvent>();
  private readonly activeBlobUrls = new Set<string>();

  public fileEvents$ = this.fileSubject.asObservable();

  // ═══════════════════════════════════════════════════════════════════════
  // VALIDATION
  // ═══════════════════════════════════════════════════════════════════════

  validateFile(file: File, config: FileUploadConfig): FileValidationResult {
    const errors: string[] = [];

    if (config.maxSize && file.size > config.maxSize) {
      errors.push(
        `File size (${this.formatFileSize(file.size)}) exceeds maximum (${this.formatFileSize(config.maxSize)})`
      );
    }

    const extension = this.getFileExtension(file.name);
    if (
      config.allowedExtensions &&
      config.allowedExtensions.length > 0 &&
      !config.allowedExtensions.includes(extension.toLowerCase())
    ) {
      errors.push(
        `File type "${extension}" not allowed. Allowed: ${config.allowedExtensions.join(', ')}`
      );
    }

    if (
      config.allowedMimeTypes &&
      config.allowedMimeTypes.length > 0 &&
      !config.allowedMimeTypes.includes(file.type)
    ) {
      errors.push(`MIME type "${file.type}" not allowed.`);
    }

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

    return { valid: errors.length === 0, errors };
  }

  validateFiles(files: File[], config: FileUploadConfig): FileValidationResult {
    const errors: string[] = [];

    if (config.maxFiles && files.length > config.maxFiles) {
      errors.push(`Maximum ${config.maxFiles} file(s) allowed. Selected: ${files.length}`);
      return { valid: false, errors };
    }

    files.forEach((file, index) => {
      const validation = this.validateFile(file, config);
      if (!validation.valid) {
        errors.push(`File ${index + 1} (${file.name}): ${validation.errors.join(', ')}`);
      }
    });

    return { valid: errors.length === 0, errors };
  }

  // ═══════════════════════════════════════════════════════════════════════
  // FILE PROCESSING - FIXED (SSR-safe)
  // ═══════════════════════════════════════════════════════════════════════

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
        this.toast.error(`Failed to process file: ${file.name}`);
      }
    }

    return metadata;
  }

  private async createFileMetadata(file: File, config: FileUploadConfig): Promise<FileMetadata> {
    const id = IdGenerator.generateUUID();
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

    // Generate preview (SSR-safe)
    if (this.isImage(file) && config.generateThumbnails) {
      // FIX: Guard DOM operations
      if (this.isBrowser) {
        try {
          metadata.preview = await this.generateImagePreview(file, config);
          if (metadata.preview.startsWith('blob:')) {
            this.activeBlobUrls.add(metadata.preview);
          }
        } catch {
          this.logger.warn('Failed to generate image preview', {
            log: { source: 'FileUploadService', type: 'application' }
          });
        }
      }
    } else if (file.type === 'application/pdf') {
      metadata.preview = 'assets/icons/pdf-icon.svg';
    }

    return metadata;
  }

  // FIX: SSR-safe image preview generation
  private async generateImagePreview(file: File, config: FileUploadConfig): Promise<string> {
    if (!this.isBrowser) {
      throw new Error('Preview generation not available in SSR');
    }

    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      
      reader.onload = (e: ProgressEvent<FileReader>) => {
        const img = new Image();
        img.onload = () => {
          try {
            const canvas = this.document.createElement('canvas');
            const ctx = canvas.getContext('2d');
            
            if (!ctx) {
              resolve(e.target?.result as string);
              return;
            }

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
            ctx.drawImage(img, 0, 0, width, height);

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

  // ═══════════════════════════════════════════════════════════════════════
  // FILE CONVERSION
  // ═══════════════════════════════════════════════════════════════════════

  async fileToBase64(file: File): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => resolve(reader.result as string);
      reader.onerror = () => reject(new Error('Failed to read file'));
      reader.readAsDataURL(file);
    });
  }

  // FIX: SSR-safe image compression
  async compressImage(file: File, quality: number = 0.8): Promise<Blob> {
    if (!this.isBrowser) {
      return file; // Return original in SSR
    }

    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      
      reader.onload = (e: ProgressEvent<FileReader>) => {
        const img = new Image();
        img.onload = () => {
          try {
            const canvas = this.document.createElement('canvas');
            const ctx = canvas.getContext('2d');
            
            if (!ctx) {
              resolve(file);
              return;
            }

            canvas.width = img.width;
            canvas.height = img.height;
            ctx.drawImage(img, 0, 0);

            canvas.toBlob(
              (blob) => resolve(blob || file),
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

  // ═══════════════════════════════════════════════════════════════════════
  // UPLOAD - IMPROVED
  // ═══════════════════════════════════════════════════════════════════════

  async uploadFile(metadata: FileMetadata, endpoint: string): Promise<string> {
    try {
      metadata.uploading = true;
      metadata.progress = 0;
      this.fileSubject.next({ type: 'progress', file: metadata });

      this.logger.info(`Starting file upload: ${metadata.name}`, {
        log: { source: 'FileUploadService', type: 'application' }
      });

      const base64Data = await this.fileToBase64(metadata.file);

      const dto: FileUploadDto = {
        name: metadata.name,
        extension: metadata.extension,
        data: base64Data,
      };

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
            
            this.logger.error(`File upload failed: ${metadata.name}`, error, {
              log: { source: 'FileUploadService', type: 'application' }
            });
            
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

      this.logger.info(`File uploaded successfully: ${metadata.name}`, {
        log: { source: 'FileUploadService', type: 'application' }
      });
      
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
   * Upload multiple files - FIXED
   * Returns detailed results with both successes and failures
   */
  async uploadFiles(
    files: FileMetadata[],
    endpoint: string,
  ): Promise<FileUploadResult> {
    const results: FileUploadResult = {
      successful: [],
      failed: [],
      totalFiles: files.length,
    };

    for (const file of files) {
      if (file.uploaded && file.url) {
        // Already uploaded
        results.successful.push({
          fileId: file.id,
          fileName: file.name,
          url: file.url,
        });
        continue;
      }

      try {
        const url = await this.uploadFile(file, endpoint);
        results.successful.push({
          fileId: file.id,
          fileName: file.name,
          url,
        });
      } catch (error) {
        // FIX: Capture error details instead of swallowing
        const errorMessage = error instanceof Error ? error.message : 'Upload failed';
        
        results.failed.push({
          fileId: file.id,
          fileName: file.name,
          error: errorMessage,
        });

        this.logger.warn(`Upload failed for ${file.name}: ${errorMessage}`, {
          log: { source: 'FileUploadService', type: 'application' }
        });
      }
    }

    // Log summary
    if (results.failed.length > 0) {
      this.logger.warn(
        `Upload completed with ${results.failed.length}/${results.totalFiles} failures`,
        {
          log: { source: 'FileUploadService', type: 'application' }
        }
      );
    }

    return results;
  }

  // ═══════════════════════════════════════════════════════════════════════
  // FILE MANAGEMENT - FIXED (SSR-safe)
  // ═══════════════════════════════════════════════════════════════════════

  removeFile(files: FileMetadata[], fileId: string): FileMetadata[] {
    const file = files.find((f) => f.id === fileId);
    if (file) {
      // FIX: Guard blob URL operations
      if (this.isBrowser && file.preview && this.activeBlobUrls.has(file.preview)) {
        URL.revokeObjectURL(file.preview);
        this.activeBlobUrls.delete(file.preview);
      }
      this.fileSubject.next({ type: 'remove', file });
    }
    return files.filter((f) => f.id !== fileId);
  }

  clearFiles(files: FileMetadata[]): void {
    if (!this.isBrowser) return;

    files.forEach((file) => {
      if (file.preview && this.activeBlobUrls.has(file.preview)) {
        URL.revokeObjectURL(file.preview);
        this.activeBlobUrls.delete(file.preview);
      }
    });
  }

  cleanup(): void {
    if (!this.isBrowser) return;

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
    this.fileSubject.complete();
  }

  // ═══════════════════════════════════════════════════════════════════════
  // UTILITIES
  // ═══════════════════════════════════════════════════════════════════════

  private getFileExtension(fileName: string): string {
    const lastDot = fileName.lastIndexOf('.');
    return lastDot === -1 ? '' : fileName.substring(lastDot);
  }

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

  private isImage(file: File): boolean {
    return file.type.startsWith('image/');
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
  }

  getDefaultConfig(fileType: FileType): FileUploadConfig {
    return { ...DEFAULT_FILE_CONFIGS[fileType] };
  }
}

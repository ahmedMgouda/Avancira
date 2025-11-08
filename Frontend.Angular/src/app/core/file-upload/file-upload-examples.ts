// /**
//  * File Upload Service - Usage Examples
//  * Comprehensive examples showing all file upload features
//  */

// import { CommonModule } from '@angular/common';
// import { Component, inject, OnInit } from '@angular/core';
// import { FormBuilder, FormGroup,FormsModule, ReactiveFormsModule } from '@angular/forms';
// import { MatButtonModule } from '@angular/material/button';

// import { FileUploadComponent } from './components/file-upload/file-upload.component';

// import { DialogService } from './services/dialog.service';
// import { FileUploadService } from './services/file-upload.service';

// import {
//   FileMetadata,
//   FileType,
//   FileUploadConfig,
// } from './models/file-upload.models';

// // ===== Example 1: Basic Image Upload =====

// @Component({
//   selector: 'app-image-upload-example',
//   standalone: true,
//   imports: [CommonModule, FileUploadComponent, MatButtonModule],
//   template: `
//     <div class="container">
//       <h2>Upload Product Images</h2>
      
//       <app-file-upload
//         [fileType]="fileType"
//         (filesChanged)="onFilesChanged($event)"
//         (filesValidated)="onFilesValidated($event)">
//       </app-file-upload>

//       <button
//         mat-raised-button
//         color="primary"
//         [disabled]="files.length === 0 || !isValid"
//         (click)="uploadImages()">
//         Upload Images
//       </button>
//     </div>
//   `,
// })
// export class ImageUploadExampleComponent {
//   private fileUploadService = inject(FileUploadService);
//   private dialogService = inject(DialogService);

//   fileType = FileType.Image;
//   files: FileMetadata[] = [];
//   isValid = true;

//   onFilesChanged(files: FileMetadata[]): void {
//     this.files = files;
//   }

//   onFilesValidated(valid: boolean): void {
//     this.isValid = valid;
//   }

//   async uploadImages(): Promise<void> {
//     try {
//       const urls = await this.fileUploadService.uploadFiles(
//         this.files,
//         '/api/products/upload-images',
//         'Product'
//       );
      
//       await this.dialogService.alertSuccess(
//         `Successfully uploaded ${urls.length} image(s)!`
//       );
      
//       console.log('Uploaded URLs:', urls);
//       // Save URLs to your form or backend
//     } catch (error) {
//       await this.dialogService.alertError('Failed to upload images. Please try again.');
//     }
//   }
// }

// // ===== Example 2: Custom Configuration =====

// @Component({
//   selector: 'app-custom-config-example',
//   standalone: true,
//   imports: [CommonModule, FileUploadComponent, MatButtonModule],
//   template: `
//     <div class="container">
//       <h2>Upload Documents (Custom Config)</h2>
      
//       <app-file-upload
//         [config]="customConfig"
//         [fileType]="fileType"
//         (filesChanged)="onFilesChanged($event)">
//       </app-file-upload>

//       <button mat-raised-button color="primary" (click)="uploadDocuments()">
//         Upload Documents
//       </button>
//     </div>
//   `,
// })
// export class CustomConfigExampleComponent {
//   private fileUploadService = inject(FileUploadService);

//   fileType = FileType.Document;
//   files: FileMetadata[] = [];

//   // Custom configuration
//   customConfig: FileUploadConfig = {
//     maxSize: 5 * 1024 * 1024, // 5MB
//     maxFiles: 3,
//     allowedExtensions: ['.pdf', '.docx'],
//     allowedMimeTypes: [
//       'application/pdf',
//       'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
//     ],
//     multiple: true,
//   };

//   onFilesChanged(files: FileMetadata[]): void {
//     this.files = files;
//   }

//   async uploadDocuments(): Promise<void> {
//     const urls = await this.fileUploadService.uploadFiles(
//       this.files,
//       '/api/documents/upload',
//       'Document'
//     );
//     console.log('Uploaded documents:', urls);
//   }
// }

// // ===== Example 3: Form Integration =====

// @Component({
//   selector: 'app-form-integration-example',
//   standalone: true,
//   imports: [
//     CommonModule,
//     ReactiveFormsModule,
//     FileUploadComponent,
//     MatButtonModule,
//   ],
//   template: `
//     <div class="container">
//       <h2>User Profile Form</h2>
      
//       <form [formGroup]="profileForm" (ngSubmit)="submitForm()">
//         <div class="form-field">
//           <label>Profile Picture</label>
//           <app-file-upload
//             [config]="avatarConfig"
//             [fileType]="FileType.Image"
//             (filesChanged)="onAvatarChanged($event)">
//           </app-file-upload>
//         </div>

//         <button
//           mat-raised-button
//           color="primary"
//           type="submit"
//           [disabled]="profileForm.invalid || uploadingFiles">
//           {{ uploadingFiles ? 'Uploading...' : 'Save Profile' }}
//         </button>
//       </form>
//     </div>
//   `,
// })
// export class FormIntegrationExampleComponent implements OnInit {
//   private fb = inject(FormBuilder);
//   private fileUploadService = inject(FileUploadService);

//   FileType = FileType;
//   profileForm!: FormGroup;
//   avatarFiles: FileMetadata[] = [];
//   uploadingFiles = false;

//   avatarConfig: FileUploadConfig = {
//     maxSize: 2 * 1024 * 1024, // 2MB
//     maxFiles: 1,
//     allowedExtensions: ['.jpg', '.jpeg', '.png'],
//     multiple: false,
//     generateThumbnails: true,
//   };

//   ngOnInit(): void {
//     this.profileForm = this.fb.group({
//       name: [''],
//       email: [''],
//       avatarUrl: [''],
//     });
//   }

//   onAvatarChanged(files: FileMetadata[]): void {
//     this.avatarFiles = files;
//   }

//   async submitForm(): Promise<void> {
//     if (this.profileForm.invalid) return;

//     try {
//       this.uploadingFiles = true;

//       // Upload avatar if selected
//       if (this.avatarFiles.length > 0) {
//         const urls = await this.fileUploadService.uploadFiles(
//           this.avatarFiles,
//           '/api/users/upload-avatar',
//           'User'
//         );
//         this.profileForm.patchValue({ avatarUrl: urls[0] });
//       }

//       // Submit form data
//       console.log('Form data:', this.profileForm.value);
//       // Call your API here

//       this.uploadingFiles = false;
//     } catch (error) {
//       this.uploadingFiles = false;
//       console.error('Upload failed:', error);
//     }
//   }
// }

// // ===== Example 4: Multiple File Types =====

// @Component({
//   selector: 'app-multi-type-example',
//   standalone: true,
//   imports: [CommonModule, FileUploadComponent, MatButtonModule],
//   template: `
//     <div class="container">
//       <h2>Upload Project Files</h2>
      
//       <!-- Images -->
//       <section>
//         <h3>Project Images</h3>
//         <app-file-upload
//           [fileType]="FileType.Image"
//           (filesChanged)="imageFiles = $event">
//         </app-file-upload>
//       </section>

//       <!-- Documents -->
//       <section>
//         <h3>Project Documents</h3>
//         <app-file-upload
//           [fileType]="FileType.Document"
//           (filesChanged)="documentFiles = $event">
//         </app-file-upload>
//       </section>

//       <button mat-raised-button color="primary" (click)="uploadAll()">
//         Upload All Files
//       </button>
//     </div>
//   `,
// })
// export class MultiTypeExampleComponent {
//   private fileUploadService = inject(FileUploadService);
//   private dialogService = inject(DialogService);

//   FileType = FileType;
//   imageFiles: FileMetadata[] = [];
//   documentFiles: FileMetadata[] = [];

//   async uploadAll(): Promise<void> {
//     try {
//       // Upload images
//       const imageUrls = await this.fileUploadService.uploadFiles(
//         this.imageFiles,
//         '/api/projects/upload-images',
//         'Project'
//       );

//       // Upload documents
//       const documentUrls = await this.fileUploadService.uploadFiles(
//         this.documentFiles,
//         '/api/projects/upload-documents',
//         'Project'
//       );

//       await this.dialogService.alertSuccess(
//         `Uploaded ${imageUrls.length} images and ${documentUrls.length} documents!`
//       );
//     } catch (error) {
//       await this.dialogService.alertError('Upload failed!');
//     }
//   }
// }

// // ===== Example 5: Progress Tracking =====

// @Component({
//   selector: 'app-progress-tracking-example',
//   standalone: true,
//   imports: [CommonModule, FileUploadComponent, MatButtonModule],
//   template: `
//     <div class="container">
//       <h2>Upload with Progress Tracking</h2>
      
//       <app-file-upload
//         [fileType]="FileType.Image"
//         (filesChanged)="files = $event">
//       </app-file-upload>

//       <!-- Overall Progress -->
//       @if (uploading) {
//         <div class="progress-container">
//           <h3>Upload Progress</h3>
//           <p>{{ uploadedCount }} of {{ totalCount }} files uploaded</p>
//           <mat-progress-bar
//             mode="determinate"
//             [value]="overallProgress">
//           </mat-progress-bar>
//         </div>
//       }

//       <button
//         mat-raised-button
//         color="primary"
//         [disabled]="files.length === 0 || uploading"
//         (click)="uploadWithTracking()">
//         {{ uploading ? 'Uploading...' : 'Upload Files' }}
//       </button>
//     </div>
//   `,
// })
// export class ProgressTrackingExampleComponent implements OnInit {
//   private fileUploadService = inject(FileUploadService);

//   FileType = FileType;
//   files: FileMetadata[] = [];
//   uploading = false;
//   uploadedCount = 0;
//   totalCount = 0;
//   overallProgress = 0;

//   ngOnInit(): void {
//     // Subscribe to upload events
//     this.fileUploadService.fileEvents$.subscribe((event) => {
//       if (event.type === 'complete') {
//         this.uploadedCount++;
//         this.overallProgress = (this.uploadedCount / this.totalCount) * 100;
//       } else if (event.type === 'error') {
//         console.error('Upload error:', event.file.error);
//       }
//     });
//   }

//   async uploadWithTracking(): Promise<void> {
//     this.uploading = true;
//     this.uploadedCount = 0;
//     this.totalCount = this.files.length;
//     this.overallProgress = 0;

//     try {
//       await this.fileUploadService.uploadFiles(
//         this.files,
//         '/api/upload',
//         'File'
//       );
//       console.log('All files uploaded successfully!');
//     } catch (error) {
//       console.error('Upload failed:', error);
//     } finally {
//       this.uploading = false;
//     }
//   }
// }

// // ===== Example 6: Single File Upload (Avatar) =====

// @Component({
//   selector: 'app-avatar-upload-example',
//   standalone: true,
//   imports: [CommonModule, FileUploadComponent, MatButtonModule],
//   template: `
//     <div class="avatar-container">
//       @if (currentAvatarUrl) {
//         <img [src]="currentAvatarUrl" alt="Avatar" class="avatar-preview" />
//       }
      
//       <app-file-upload
//         [config]="avatarConfig"
//         [fileType]="FileType.Image"
//         (filesChanged)="onAvatarChanged($event)">
//       </app-file-upload>

//       <button
//         mat-raised-button
//         color="primary"
//         [disabled]="!hasNewAvatar"
//         (click)="uploadAvatar()">
//         Update Avatar
//       </button>
//     </div>
//   `,
// })
// export class AvatarUploadExampleComponent {
//   private fileUploadService = inject(FileUploadService);

//   FileType = FileType;
//   currentAvatarUrl = 'https://example.com/avatar.jpg';
//   avatarFiles: FileMetadata[] = [];

//   avatarConfig: FileUploadConfig = {
//     maxSize: 2 * 1024 * 1024,
//     maxFiles: 1,
//     allowedExtensions: ['.jpg', '.jpeg', '.png'],
//     multiple: false,
//     compressImages: true,
//     compressionQuality: 0.7,
//   };

//   get hasNewAvatar(): boolean {
//     return this.avatarFiles.length > 0;
//   }

//   onAvatarChanged(files: FileMetadata[]): void {
//     this.avatarFiles = files;
//   }

//   async uploadAvatar(): Promise<void> {
//     if (this.avatarFiles.length === 0) return;

//     const url = await this.fileUploadService.uploadFile(
//       this.avatarFiles[0],
//       '/api/users/avatar',
//       'User'
//     );

//     this.currentAvatarUrl = url;
//     console.log('Avatar updated:', url);
//   }
// }

// // ===== Example 7: Validation Example =====

// @Component({
//   selector: 'app-validation-example',
//   standalone: true,
//   imports: [CommonModule, FileUploadComponent],
//   template: `
//     <div class="container">
//       <h2>File Validation Example</h2>
      
//       <app-file-upload
//         [config]="strictConfig"
//         [fileType]="FileType.Document"
//         (filesChanged)="files = $event"
//         (filesValidated)="onValidation($event)">
//       </app-file-upload>

//       @if (!isValid) {
//         <div class="validation-message">
//           Please fix the validation errors before uploading.
//         </div>
//       }
//     </div>
//   `,
// })
// export class ValidationExampleComponent {
//   FileType = FileType;
//   files: FileMetadata[] = [];
//   isValid = true;

//   strictConfig: FileUploadConfig = {
//     maxSize: 1 * 1024 * 1024, // 1MB
//     maxFiles: 2,
//     allowedExtensions: ['.pdf'],
//     allowedMimeTypes: ['application/pdf'],
//   };

//   onValidation(valid: boolean): void {
//     this.isValid = valid;
//     console.log('Files valid:', valid);
//   }
// }
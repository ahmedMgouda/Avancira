/**
 * File Upload Models & Types
 * Provides strong typing for file upload configurations and validation
 */

/**
 * Supported file types matching backend FileType enum
 */
export enum FileType {
  Image = 'Image',
  Document = 'Document',
  Video = 'Video',
  Audio = 'Audio',
  Other = 'Other',
}

/**
 * File upload configuration
 */
export interface FileUploadConfig {
  /** Maximum file size in bytes */
  maxSize?: number;
  /** Maximum number of files allowed */
  maxFiles?: number;
  /** Allowed file extensions (e.g., ['.jpg', '.png']) */
  allowedExtensions?: string[];
  /** Allowed MIME types (e.g., ['image/jpeg', 'image/png']) */
  allowedMimeTypes?: string[];
  /** Whether to allow multiple file selection */
  multiple?: boolean;
  /** File type category */
  fileType?: FileType;
  /** Whether to generate thumbnails for images */
  generateThumbnails?: boolean;
  /** Thumbnail max width */
  thumbnailMaxWidth?: number;
  /** Thumbnail max height */
  thumbnailMaxHeight?: number;
  /** Whether to compress images before upload */
  compressImages?: boolean;
  /** Image compression quality (0-1) */
  compressionQuality?: number;
}

/**
 * File metadata for upload
 */
export interface FileMetadata {
  /** Unique identifier */
  id: string;
  /** Original file */
  file: File;
  /** File name */
  name: string;
  /** File extension */
  extension: string;
  /** File size in bytes */
  size: number;
  /** MIME type */
  type: string;
  /** Preview URL (data URL or blob URL) */
  preview?: string;
  /** Upload progress (0-100) */
  progress?: number;
  /** Whether file is uploading */
  uploading?: boolean;
  /** Whether upload completed successfully */
  uploaded?: boolean;
  /** Upload error message */
  error?: string;
  /** Backend response URL */
  url?: string;
  /** Base64 data for backend */
  data?: string;
}

/**
 * File validation result
 */
export interface FileValidationResult {
  valid: boolean;
  errors: string[];
}

/**
 * File upload request matching backend DTO
 */
export interface FileUploadDto {
  name: string;
  extension: string;
  data: string; // Base64 with data URL prefix
}

/**
 * Result from uploading multiple files
 * Provides detailed success and failure information
 */
export interface FileUploadResult {
  /** Successfully uploaded files */
  successful: Array<{
    fileId: string;
    fileName: string;
    url: string;
  }>;
  /** Failed uploads with error details */
  failed: Array<{
    fileId: string;
    fileName: string;
    error: string;
  }>;
  /** Total number of files attempted */
  totalFiles: number;
}

/**
 * Default file upload configurations
 */
export const DEFAULT_FILE_CONFIGS: Record<FileType, FileUploadConfig> = {
  [FileType.Image]: {
    maxSize: 5 * 1024 * 1024, // 5MB
    maxFiles: 10,
    allowedExtensions: ['.jpg', '.jpeg', '.png', '.gif', '.webp', '.svg'],
    allowedMimeTypes: ['image/jpeg', 'image/png', 'image/gif', 'image/webp', 'image/svg+xml'],
    generateThumbnails: true,
    thumbnailMaxWidth: 300,
    thumbnailMaxHeight: 300,
    compressImages: true,
    compressionQuality: 0.8,
  },
  [FileType.Document]: {
    maxSize: 10 * 1024 * 1024, // 10MB
    maxFiles: 5,
    allowedExtensions: ['.pdf', '.doc', '.docx', '.xls', '.xlsx', '.txt'],
    allowedMimeTypes: [
      'application/pdf',
      'application/msword',
      'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
      'application/vnd.ms-excel',
      'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      'text/plain',
    ],
    generateThumbnails: false,
  },
  [FileType.Video]: {
    maxSize: 50 * 1024 * 1024, // 50MB
    maxFiles: 3,
    allowedExtensions: ['.mp4', '.avi', '.mov', '.wmv', '.webm'],
    allowedMimeTypes: ['video/mp4', 'video/x-msvideo', 'video/quicktime', 'video/x-ms-wmv', 'video/webm'],
    generateThumbnails: false,
  },
  [FileType.Audio]: {
    maxSize: 20 * 1024 * 1024, // 20MB
    maxFiles: 5,
    allowedExtensions: ['.mp3', '.wav', '.ogg', '.m4a'],
    allowedMimeTypes: ['audio/mpeg', 'audio/wav', 'audio/ogg', 'audio/mp4'],
    generateThumbnails: false,
  },
  [FileType.Other]: {
    maxSize: 20 * 1024 * 1024, // 20MB
    maxFiles: 10,
    allowedExtensions: [],
    allowedMimeTypes: [],
    generateThumbnails: false,
  },
};

/**
 * File upload event types
 */
export interface FileUploadEvent {
  type: 'add' | 'remove' | 'progress' | 'complete' | 'error';
  file: FileMetadata;
}

/**
 * Common MIME type mappings
 */
export const MIME_TYPE_EXTENSIONS: Record<string, string> = {
  'image/jpeg': '.jpg',
  'image/png': '.png',
  'image/gif': '.gif',
  'image/webp': '.webp',
  'image/svg+xml': '.svg',
  'application/pdf': '.pdf',
  'application/msword': '.doc',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document': '.docx',
  'application/vnd.ms-excel': '.xls',
  'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet': '.xlsx',
  'text/plain': '.txt',
  'video/mp4': '.mp4',
  'audio/mpeg': '.mp3',
};

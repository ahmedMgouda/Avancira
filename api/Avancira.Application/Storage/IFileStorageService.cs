using Microsoft.AspNetCore.Http;

namespace Avancira.Application.Storage;

/// <summary>
/// Unified file storage service supporting multiple storage backends
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Upload a file using a framework-agnostic representation
    /// </summary>
    /// <param name="fileData">File data to upload</param>
    /// <param name="options">Upload configuration options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Public URL or relative path to access the file</returns>
    Task<string> UploadAsync(
        FileData fileData,
        FileUploadOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload a file from base64 string (for images from frontend)
    /// </summary>
    Task<string> UploadBase64Async(
        string base64Data,
        FileUploadOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a file by its path/URL
    /// </summary>
    Task DeleteAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a file exists
    /// </summary>
    Task<bool> ExistsAsync(string filePath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents file data in a framework-agnostic way
/// </summary>
public sealed class FileData : IDisposable
{
    /// <summary>
    /// File content as stream
    /// </summary>
    public Stream Content { get; init; } = default!;

    /// <summary>
    /// Original filename (e.g., avatar.png)
    /// </summary>
    public string FileName { get; init; } = default!;

    /// <summary>
    /// MIME type (e.g., image/png)
    /// </summary>
    public string ContentType { get; init; } = default!;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long Length => Content?.Length ?? 0;

    /// <summary>
    /// File extension (e.g., .png)
    /// </summary>
    public string Extension => Path.GetExtension(FileName).ToLowerInvariant();

    public void Dispose()
    {
        Content?.Dispose();
    }

    /// <summary>
    /// Create FileData from ASP.NET Core IFormFile
    /// </summary>
    public static async Task<FileData> FromFormFileAsync(
        IFormFile formFile,
        CancellationToken cancellationToken = default)
    {
        if (formFile == null || formFile.Length == 0)
            throw new ArgumentException("Form file is null or empty", nameof(formFile));

        var memoryStream = new MemoryStream();
        await formFile.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        return new FileData
        {
            Content = memoryStream,
            FileName = formFile.FileName,
            ContentType = formFile.ContentType
        };
    }

    /// <summary>
    /// Create FileData from base64 string
    /// </summary>
    public static FileData FromBase64(string base64Data, string fileName, string? contentType = null)
    {
        if (string.IsNullOrWhiteSpace(base64Data))
            throw new ArgumentException("Base64 data is null or empty", nameof(base64Data));

        // Extract base64 content (remove data:image/png;base64, prefix if present)
        var base64Match = System.Text.RegularExpressions.Regex.Match(
            base64Data, @"data:(?<type>[^;]+);base64,(?<data>.+)");

        var base64Content = base64Match.Success
            ? base64Match.Groups["data"].Value
            : base64Data;

        var detectedContentType = base64Match.Success
            ? base64Match.Groups["type"].Value
            : contentType ?? "application/octet-stream";

        var bytes = Convert.FromBase64String(base64Content);
        var stream = new MemoryStream(bytes);

        return new FileData
        {
            Content = stream,
            FileName = fileName,
            ContentType = detectedContentType
        };
    }

    /// <summary>
    /// Create FileData from byte array
    /// </summary>
    public static FileData FromBytes(byte[] bytes, string fileName, string contentType)
    {
        return new FileData
        {
            Content = new MemoryStream(bytes),
            FileName = fileName,
            ContentType = contentType
        };
    }

    /// <summary>
    /// Create FileData from stream
    /// </summary>
    public static FileData FromStream(Stream stream, string fileName, string contentType)
    {
        return new FileData
        {
            Content = stream,
            FileName = fileName,
            ContentType = contentType
        };
    }
}

/// <summary>
/// File upload configuration options
/// </summary>
public class FileUploadOptions
{
    /// <summary>
    /// Subdirectory under the base path (e.g., "subjects", "users", "courses")
    /// </summary>
    public string? SubDirectory { get; set; }

    /// <summary>
    /// Original filename (optional, will be sanitized)
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Maximum file size in bytes (default: 10MB)
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Allowed file extensions (e.g., [".jpg", ".png"])
    /// </summary>
    public string[]? AllowedExtensions { get; set; }

    /// <summary>
    /// Allowed MIME types (e.g., ["image/jpeg", "image/png"])
    /// </summary>
    public string[]? AllowedMimeTypes { get; set; }

    /// <summary>
    /// Validate MIME type by inspecting file content (magic bytes)
    /// Highly recommended for security
    /// </summary>
    public bool ValidateMimeTypeFromContent { get; set; } = true;

    /// <summary>
    /// Generate unique filename (default: true)
    /// </summary>
    public bool GenerateUniqueFileName { get; set; } = true;

    /// <summary>
    /// Preserve original extension (default: true)
    /// </summary>
    public bool PreserveExtension { get; set; } = true;

    // ═══════════════════════════════════════════════════════════════
    // Preset Configurations
    // ═══════════════════════════════════════════════════════════════

    public static FileUploadOptions ForImages(string subDirectory) => new()
    {
        SubDirectory = subDirectory,
        AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg" },
        AllowedMimeTypes = new[]
        {
            "image/jpeg",
            "image/png",
            "image/gif",
            "image/webp",
            "image/svg+xml"
        },
        MaxFileSizeBytes = 5 * 1024 * 1024, // 5MB
        ValidateMimeTypeFromContent = true
    };

    public static FileUploadOptions ForDocuments(string subDirectory) => new()
    {
        SubDirectory = subDirectory,
        AllowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt", ".xlsx", ".xls" },
        AllowedMimeTypes = new[]
        {
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "text/plain",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        },
        MaxFileSizeBytes = 20 * 1024 * 1024, // 20MB
        ValidateMimeTypeFromContent = true
    };

    public static FileUploadOptions ForAvatars(string subDirectory = "avatars") => new()
    {
        SubDirectory = subDirectory,
        AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" },
        AllowedMimeTypes = new[] { "image/jpeg", "image/png", "image/webp" },
        MaxFileSizeBytes = 2 * 1024 * 1024, // 2MB
        ValidateMimeTypeFromContent = true
    };

    public static FileUploadOptions ForVideos(string subDirectory) => new()
    {
        SubDirectory = subDirectory,
        AllowedExtensions = new[] { ".mp4", ".webm", ".mov", ".avi" },
        AllowedMimeTypes = new[]
        {
            "video/mp4",
            "video/webm",
            "video/quicktime",
            "video/x-msvideo"
        },
        MaxFileSizeBytes = 100 * 1024 * 1024, // 100MB
        ValidateMimeTypeFromContent = true
    };
}

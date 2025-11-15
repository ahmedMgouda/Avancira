using Avancira.Application.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace Avancira.Infrastructure.Storage;

/// <summary>
/// Local file system storage implementation with MIME type validation
/// Stores files in: {ContentRoot}/storage/{subdirectory}/{filename}
/// Serves files via: /api/files/{subdirectory}/{filename}
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly FileStorageSettings _settings;
    private readonly string _storageRootPath;

    public LocalFileStorageService(
        IWebHostEnvironment environment,
        ILogger<LocalFileStorageService> logger,
        IOptions<FileStorageSettings> settings)
    {
        _environment = environment;
        _logger = logger;
        _settings = settings.Value;

        // Storage root: {ContentRoot}/storage
        _storageRootPath = Path.Combine(
            _environment.ContentRootPath,
            _settings.StorageDirectory ?? "storage");

        EnsureStorageDirectoryExists();
    }

    public async Task<string> UploadAsync(
        FileData fileData,
        FileUploadOptions options,
        CancellationToken cancellationToken = default)
    {
        if (fileData == null)
            throw new ArgumentNullException(nameof(fileData));

        if (fileData.Content == null || fileData.Length == 0)
            throw FileValidationException.EmptyFile();

        try
        {
            // Validate file
            await ValidateFileAsync(fileData, options, cancellationToken);

            // Determine target directory
            var targetDirectory = GetTargetDirectory(options.SubDirectory);

            // Generate filename
            var fileName = GenerateFileName(fileData.FileName, options);
            var filePath = Path.Combine(targetDirectory, fileName);

            // Save file
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fileData.Content.Position = 0;
                await fileData.Content.CopyToAsync(fileStream, cancellationToken);
            }

            _logger.LogInformation(
                "File uploaded successfully: {FileName} ({Size} bytes) to {Path}",
                fileName, fileData.Length, filePath);

            // Return public URL
            return GeneratePublicUrl(options.SubDirectory, fileName);
        }
        catch (FileStorageException)
        {
            // Re-throw our own exceptions
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file: {FileName}", fileData.FileName);
            throw FileOperationException.UploadFailed(fileData.FileName, ex);
        }
    }

    public async Task<string> UploadBase64Async(
        string base64Data,
        FileUploadOptions options,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(base64Data))
            throw FileValidationException.EmptyFile();

        try
        {
            // Convert base64 to FileData
            var fileName = options.FileName ?? $"upload{Guid.NewGuid():N}";
            using var fileData = FileData.FromBase64(base64Data, fileName);

            // Upload using the main upload method
            return await UploadAsync(fileData, options, cancellationToken);
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Invalid base64 format");
            throw new InvalidBase64Exception(ex);
        }
        catch (FileStorageException)
        {
            // Re-throw our own exceptions
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload base64 file");
            throw FileOperationException.UploadFailed("base64 file", ex);
        }
    }

    public async Task DeleteAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        try
        {
            // Convert public URL to physical path
            var physicalPath = GetPhysicalPath(filePath);

            if (File.Exists(physicalPath))
            {
                await Task.Run(() => File.Delete(physicalPath), cancellationToken);
                _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
            }
            else
            {
                _logger.LogWarning("File not found for deletion: {FilePath}", filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {FilePath}", filePath);
            throw FileOperationException.DeleteFailed(filePath, ex);
        }
    }

    public Task<bool> ExistsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return Task.FromResult(false);

        var physicalPath = GetPhysicalPath(filePath);
        return Task.FromResult(File.Exists(physicalPath));
    }

    // ═══════════════════════════════════════════════════════════════
    // Private Helper Methods
    // ═══════════════════════════════════════════════════════════════

    private async Task ValidateFileAsync(FileData fileData, FileUploadOptions options, CancellationToken cancellationToken)
    {
        // Validate filename
        if (string.IsNullOrWhiteSpace(fileData.FileName))
            throw FileValidationException.InvalidFileName();

        // Validate size
        if (fileData.Length > options.MaxFileSizeBytes)
            throw FileValidationException.FileSizeExceeded(fileData.Length, options.MaxFileSizeBytes);

        // Validate extension
        if (options.AllowedExtensions?.Length > 0)
        {
            var extension = fileData.Extension;
            if (string.IsNullOrEmpty(extension) || !options.AllowedExtensions.Contains(extension))
                throw FileValidationException.InvalidExtension(extension, options.AllowedExtensions);
        }

        // Validate MIME type from claimed ContentType
        if (options.AllowedMimeTypes?.Length > 0)
        {
            if (string.IsNullOrWhiteSpace(fileData.ContentType) ||
                !options.AllowedMimeTypes.Contains(fileData.ContentType, StringComparer.OrdinalIgnoreCase))
            {
                throw new FileMimeTypeException(fileData.ContentType, options.AllowedMimeTypes);
            }
        }

        // Validate MIME type from content (magic bytes) for security
        if (options.ValidateMimeTypeFromContent && fileData.Content.CanSeek)
        {
            var detectedMimeType = await MimeTypeDetector.DetectMimeTypeAsync(fileData.Content);

            if (detectedMimeType == null)
            {
                _logger.LogWarning(
                    "Could not detect MIME type from content for file: {FileName}",
                    fileData.FileName);

                // If we can't detect it but it claims to be text/plain or an allowed type, allow it
                if (options.AllowedMimeTypes?.Contains("text/plain") != true &&
                    options.AllowedMimeTypes?.Contains(fileData.ContentType, StringComparer.OrdinalIgnoreCase) != true)
                {
                    throw new FileMimeTypeException(
                        "Unable to verify file type from content",
                        new[] { "The file content could not be verified as a valid file type" });
                }
            }
            else
            {
                // Verify detected MIME type matches claimed type
                var isValid = await MimeTypeDetector.ValidateMimeTypeAsync(fileData.Content, fileData.ContentType);

                if (!isValid)
                {
                    _logger.LogWarning(
                        "MIME type mismatch for file {FileName}. Claimed: {ClaimedType}, Detected: {DetectedType}",
                        fileData.FileName, fileData.ContentType, detectedMimeType);

                    throw new FileMimeTypeMismatchException(
                        fileData.ContentType,
                        detectedMimeType,
                        fileData.FileName);
                }

                // Verify detected MIME type is allowed
                if (options.AllowedMimeTypes?.Length > 0 &&
                    !options.AllowedMimeTypes.Contains(detectedMimeType, StringComparer.OrdinalIgnoreCase))
                {
                    throw new FileMimeTypeException(detectedMimeType, options.AllowedMimeTypes);
                }
            }
        }
    }

    private string GetTargetDirectory(string? subDirectory)
    {
        var targetPath = string.IsNullOrWhiteSpace(subDirectory)
            ? _storageRootPath
            : Path.Combine(_storageRootPath, SanitizePath(subDirectory));

        if (!Directory.Exists(targetPath))
            Directory.CreateDirectory(targetPath);

        return targetPath;
    }

    private string GenerateFileName(string originalFileName, FileUploadOptions options)
    {
        var extension = options.PreserveExtension
            ? Path.GetExtension(originalFileName).ToLowerInvariant()
            : string.Empty;

        if (options.GenerateUniqueFileName)
        {
            return $"{Guid.NewGuid()}{extension}";
        }

        var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
        var sanitizedName = SanitizeFileName(nameWithoutExtension);
        return $"{sanitizedName}{extension}";
    }

    private string GeneratePublicUrl(string? subDirectory, string fileName)
    {
        // URL format: /api/files/{subdirectory}/{filename}
        var urlPath = string.IsNullOrWhiteSpace(subDirectory)
            ? $"{_settings.PublicUrlPrefix}/{fileName}"
            : $"{_settings.PublicUrlPrefix}/{subDirectory}/{fileName}";

        return urlPath.Replace("\\", "/");
    }

    private string GetPhysicalPath(string publicPath)
    {
        // Remove the public URL prefix (/api/files)
        var relativePath = publicPath.Replace(_settings.PublicUrlPrefix, "", StringComparison.OrdinalIgnoreCase)
            .TrimStart('/', '\\');

        return Path.Combine(_storageRootPath, relativePath);
    }

    private void EnsureStorageDirectoryExists()
    {
        if (!Directory.Exists(_storageRootPath))
        {
            Directory.CreateDirectory(_storageRootPath);
            _logger.LogInformation("Created storage directory: {Path}", _storageRootPath);
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        // Remove special characters, keep only alphanumeric, dash, underscore, dot
        var sanitized = Regex.Replace(fileName, @"[^a-zA-Z0-9\-_\.]", "_");
        return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized;
    }

    private static string SanitizePath(string path)
    {
        // Remove dangerous path characters
        return Regex.Replace(path, @"[^a-zA-Z0-9\-_/\\]", "_");
    }
}

/// <summary>
/// File storage configuration settings
/// </summary>
public class FileStorageSettings
{
    public string StorageDirectory { get; set; } = "storage";
    public string PublicUrlPrefix { get; set; } = "/api/files";
}
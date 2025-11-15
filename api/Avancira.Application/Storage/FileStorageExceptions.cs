using Avancira.Domain.Common.Exceptions;
using System.Net;

namespace Avancira.Application.Storage;

/// <summary>
/// Base exception for file storage operations
/// </summary>
public class FileStorageException : AvanciraException
{
    public FileStorageException(
        string message,
        IEnumerable<string>? errors = null,
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError,
        string? errorCode = null)
        : base(message, errors, statusCode, errorCode)
    {
    }

    public FileStorageException(
        string message,
        Exception innerException,
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError,
        string? errorCode = null)
        : base(message, new[] { innerException.Message }, statusCode, errorCode)
    {
    }
}

/// <summary>
/// Exception thrown when file validation fails (size, extension, etc.)
/// </summary>
public class FileValidationException : FileStorageException
{
    public FileValidationException(string message, IEnumerable<string>? errors = null)
        : base(message, errors, HttpStatusCode.BadRequest, "FILE_VALIDATION_FAILED")
    {
    }

    public static FileValidationException FileSizeExceeded(long actualSize, long maxSize)
    {
        return new FileValidationException(
            "File size exceeds the maximum allowed size.",
            new[]
            {
                $"Actual size: {FormatBytes(actualSize)}",
                $"Maximum allowed: {FormatBytes(maxSize)}"
            });
    }

    public static FileValidationException InvalidExtension(string extension, string[] allowedExtensions)
    {
        return new FileValidationException(
            $"File extension '{extension}' is not allowed.",
            new[]
            {
                $"Allowed extensions: {string.Join(", ", allowedExtensions)}"
            });
    }

    public static FileValidationException EmptyFile()
    {
        return new FileValidationException("File is empty or has zero length.");
    }

    public static FileValidationException InvalidFileName()
    {
        return new FileValidationException("File name is invalid or missing.");
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

/// <summary>
/// Exception thrown when a file operation fails (write, delete, etc.)
/// </summary>
public class FileOperationException : FileStorageException
{
    public FileOperationException(
        string message,
        Exception innerException)
        : base(message, innerException, HttpStatusCode.InternalServerError, "FILE_OPERATION_FAILED")
    {
    }

    public static FileOperationException UploadFailed(string fileName, Exception innerException)
    {
        return new FileOperationException(
            $"Failed to upload file '{fileName}'.",
            innerException);
    }

    public static FileOperationException DeleteFailed(string filePath, Exception innerException)
    {
        return new FileOperationException(
            $"Failed to delete file '{filePath}'.",
            innerException);
    }
}

/// <summary>
/// Exception thrown when a file is not found
/// </summary>
public class FileNotFoundException : FileStorageException
{
    public FileNotFoundException(string filePath)
        : base(
            $"File not found: '{filePath}'",
            new[] { "The requested file does not exist or has been deleted." },
            HttpStatusCode.NotFound,
            "FILE_NOT_FOUND")
    {
    }
}

/// <summary>
/// Exception thrown when base64 data is invalid
/// </summary>
public class InvalidBase64Exception : FileValidationException
{
    public InvalidBase64Exception(Exception innerException)
        : base(
            "Invalid base64 data format.",
            new[]
            {
                "The provided base64 string could not be decoded.",
                innerException.Message
            })
    {
    }
}

/// <summary>
/// Exception thrown when MIME type is not allowed
/// </summary>
public class FileMimeTypeException : FileValidationException
{
    public FileMimeTypeException(string mimeType, string[] allowedMimeTypes)
        : base(
            $"File MIME type '{mimeType}' is not allowed.",
            new[]
            {
                $"Allowed MIME types: {string.Join(", ", allowedMimeTypes)}"
            })
    {
    }
    public FileMimeTypeException(string message, IEnumerable<string> errors)
    : base(message, errors)
    {
    }
}

/// <summary>
/// Exception thrown when detected MIME type doesn't match claimed MIME type
/// </summary>
public class FileMimeTypeMismatchException : FileValidationException
{
    public string ClaimedMimeType { get; }
    public string DetectedMimeType { get; }
    public string FileName { get; }

    public FileMimeTypeMismatchException(
        string claimedMimeType,
        string detectedMimeType,
        string fileName)
        : base(
            "File content does not match the claimed file type.",
            new[]
            {
                $"File: {fileName}",
                $"Claimed type: {claimedMimeType}",
                $"Detected type: {detectedMimeType}",
                "This could indicate a file extension spoofing attempt or incorrect file upload."
            })
    {
        ClaimedMimeType = claimedMimeType;
        DetectedMimeType = detectedMimeType;
        FileName = fileName;
    }
}
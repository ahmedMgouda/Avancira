using Avancira.Application.Catalog;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Avancira.Infrastructure.Catalog
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<FileUploadService> _logger;
        private readonly string _uploadsPath;

        public FileUploadService(
            IWebHostEnvironment webHostEnvironment,
            ILogger<FileUploadService> logger
        )
        {
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _uploadsPath = Path.Combine(_webHostEnvironment.WebRootPath ?? _webHostEnvironment.ContentRootPath, "uploads");
            
            // Ensure uploads directory exists
            if (!Directory.Exists(_uploadsPath))
            {
                Directory.CreateDirectory(_uploadsPath);
            }
        }

        public async Task<string> SaveFileAsync(IFormFile file, string subDirectory = "")
        {
            try
            {
                if (file == null || file.Length == 0)
                    throw new ArgumentException("File is null or empty");

                // Validate file size (10MB limit)
                const long maxFileSize = 10 * 1024 * 1024; // 10MB
                if (file.Length > maxFileSize)
                    throw new ArgumentException("File size exceeds 10MB limit");

                // Validate file extension
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".txt" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!Array.Exists(allowedExtensions, ext => ext == fileExtension))
                    throw new ArgumentException($"File type {fileExtension} is not allowed");

                // Create subdirectory if specified
                var targetDirectory = _uploadsPath;
                if (!string.IsNullOrEmpty(subDirectory))
                {
                    targetDirectory = Path.Combine(_uploadsPath, subDirectory);
                    if (!Directory.Exists(targetDirectory))
                    {
                        Directory.CreateDirectory(targetDirectory);
                    }
                }

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(targetDirectory, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return relative path for storage in database
                var relativePath = string.IsNullOrEmpty(subDirectory) 
                    ? $"uploads/{fileName}" 
                    : $"uploads/{subDirectory}/{fileName}";

                _logger.LogInformation("File saved successfully: {FilePath}", relativePath);
                return relativePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file: {FileName}", file?.FileName);
                throw;
            }
        }

        public async Task<string?> ReplaceFileAsync(IFormFile? newFile, string? currentFilePath, string subDirectory = "")
        {
            try
            {
                // Delete current file if it exists
                if (!string.IsNullOrEmpty(currentFilePath))
                {
                    await DeleteFileAsync(currentFilePath);
                }

                // Save new file if provided
                if (newFile != null && newFile.Length > 0)
                {
                    return await SaveFileAsync(newFile, subDirectory);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error replacing file: {CurrentFilePath}", currentFilePath);
                throw;
            }
        }

        public async Task DeleteFileAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return;

                // Convert relative path to absolute path
                var absolutePath = Path.Combine(_webHostEnvironment.WebRootPath ?? _webHostEnvironment.ContentRootPath, filePath);

                if (File.Exists(absolutePath))
                {
                    File.Delete(absolutePath);
                    _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
                }
                else
                {
                    _logger.LogWarning("File not found for deletion: {FilePath}", filePath);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
                throw;
            }
        }
    }
}

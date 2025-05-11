using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Infrastructure.Storage.Files
{
    public class FileUploadService : IFileUploadService
    {
        private readonly string _baseUploadFolder;
        private readonly AppOptions _appOptions;
        private readonly ILogger<FileUploadService> _logger;

        public FileUploadService(
            IOptions<AppOptions> appOptions,
            ILogger<FileUploadService> logger
        )
        {
            _baseUploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
            _appOptions = appOptions.Value;
            _logger = logger;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string subDirectory = "")
        {
            var uploadsFolder = string.IsNullOrEmpty(subDirectory)
                ? _baseUploadFolder
                : Path.Combine(_baseUploadFolder, subDirectory);

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Generate a unique file name (using GUID)
            var fileExtension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"{_appOptions.BaseUrl}/api/uploads/{(string.IsNullOrEmpty(subDirectory) ? "" : $"{subDirectory}/")}{uniqueFileName}";
        }

        public async Task<string?> ReplaceFileAsync(IFormFile? newFile, string? currentFilePath, string subDirectory = "")
        {
            if (newFile == null)
            {
                return currentFilePath;
            }

            // Delete the current file if it exists
            if (!string.IsNullOrEmpty(currentFilePath))
            {
                await DeleteFileAsync(currentFilePath);
            }

            // Save the new file
            return await SaveFileAsync(newFile, subDirectory);
        }

        public async Task DeleteFileAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            var fullPath = Path.Combine(_baseUploadFolder, Path.GetFileName(filePath));

            if (File.Exists(fullPath))
            {
                await Task.Run(() => File.Delete(fullPath));
            }
        }
    }
}

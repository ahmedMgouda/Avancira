using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public interface IFileUploadService
{
    // Create
    Task<string> SaveFileAsync(IFormFile file, string subDirectory = "");
    // Update
    Task<string?> ReplaceFileAsync(IFormFile? newFile, string? currentFilePath, string subDirectory = "");
    // Delete
    Task DeleteFileAsync(string filePath);
}


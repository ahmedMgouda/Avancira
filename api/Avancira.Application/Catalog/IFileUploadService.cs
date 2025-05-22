using System.Threading.Tasks;
using Avancira.Application.Storage.File.Dtos;

public interface IFileUploadService
{
    // Create
    Task<string> SaveFileAsync(FileUploadDto file, string subDirectory = "");
    // Update
    Task<string?> ReplaceFileAsync(FileUploadDto? newFile, string? currentFilePath, string subDirectory = "");
    // Delete
    Task DeleteFileAsync(string filePath);
}


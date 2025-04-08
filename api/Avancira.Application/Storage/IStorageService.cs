using Avancira.Application.Storage.File;
using Avancira.Application.Storage.File.Dtos;

namespace Avancira.Application.Storage;
public interface IStorageService
{
    public Task<Uri> UploadAsync<T>(FileUploadDto? request, FileType supportedFileType, CancellationToken cancellationToken = default)
    where T : class;

    public void Remove(Uri? path);
}

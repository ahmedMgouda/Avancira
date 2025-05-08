using Avancira.Application.Storage.File.Dtos;

namespace Avancira.Application.Catalog.Categories.Dtos
{
    public class CategoryCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public bool DisplayInLandingPage { get; set; }
        public FileUploadDto? Image { get; set; }
    }
}

using Avancira.Application.Storage.File.Dtos;

namespace Avancira.Application.Catalog.Categories.Dtos
{
    public class CategoryUpdateDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool DisplayInLandingPage { get; set; }
        public FileUploadDto? Image { get; set; }
        public bool DeleteCurrentImage { get; set; }
    }
}
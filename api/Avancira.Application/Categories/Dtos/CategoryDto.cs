namespace Avancira.Application.Categories.Dtos
{
    public class CategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool DisplayInLandingPage { get; set; } = false;
        public Uri? ImageUrl { get; set; }
    }
}

namespace Avancira.BFF.Models;

public class UserProfile
{
    public string Id { get; set; } = default!;
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName => string.Join(" ", new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
    public string? ProfileImageUrl { get; set; }
    public string[] Roles { get; set; } = Array.Empty<string>();
}
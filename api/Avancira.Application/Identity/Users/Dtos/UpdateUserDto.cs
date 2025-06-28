using Avancira.Application.Storage.File.Dtos;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Avancira.Application.Identity.Users.Dtos;
public class UpdateUserDto
{
    public string Id { get; set; } = default!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? DateOfBirth { get; set; }
    public string? SkypeId { get; set; }
    public string? HangoutId { get; set; }
    public IFormFile? Image { get; set; }
    public bool DeleteCurrentImage { get; set; }
}

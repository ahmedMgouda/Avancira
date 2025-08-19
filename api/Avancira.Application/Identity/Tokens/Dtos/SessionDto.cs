namespace Avancira.Application.Identity.Tokens.Dtos;

public record SessionDto(
    Guid Id,
    string Device,
    string? UserAgent,
    string? OperatingSystem,
    string IpAddress,
    string? Country,
    string? City,
    DateTime CreatedUtc,
    DateTime LastActivityUtc,
    DateTime AbsoluteExpiryUtc,
    DateTime? RevokedUtc);

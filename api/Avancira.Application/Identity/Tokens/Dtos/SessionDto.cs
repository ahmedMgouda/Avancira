namespace Avancira.Application.Identity.Tokens.Dtos;

public record SessionDto(
    Guid Id,
    string Device,
    string? UserAgent,
    string? OperatingSystem,
    string IpAddress,
    string? Country,
    string? City,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? RevokedAt);

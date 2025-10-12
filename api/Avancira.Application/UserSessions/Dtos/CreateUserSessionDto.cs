using System;

namespace Avancira.Application.UserSessions.Dtos;

public sealed record CreateUserSessionDto(
    string UserId,
    string? DeviceId,
    string? DeviceName,
    string? UserAgent,
    string? IpAddress,
    string? RefreshTokenReferenceId,
    DateTimeOffset? TokenExpiresAt);

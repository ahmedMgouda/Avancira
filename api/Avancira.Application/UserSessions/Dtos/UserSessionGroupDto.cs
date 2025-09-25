using System.Collections.Generic;

namespace Avancira.Application.UserSessions.Dtos;

public sealed record UserSessionGroupDto(
    string DeviceId,
    string? DeviceName,
    string? OperatingSystem,
    string? UserAgent,
    string? Country,
    string? City,
    IReadOnlyList<UserSessionDto> Sessions);

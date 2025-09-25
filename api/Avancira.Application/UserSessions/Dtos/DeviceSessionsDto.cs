using System;
using System.Collections.Generic;

namespace Avancira.Application.UserSessions.Dtos;

public sealed record DeviceSessionsDto(
    string DeviceId,
    string? DeviceName,
    string? OperatingSystem,
    string? UserAgent,
    string? Country,
    string? City,
    DateTime LastActivityUtc,
    IReadOnlyList<UserSessionDto> Sessions);

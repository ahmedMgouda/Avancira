using System;
using System.Collections.Generic;

namespace Avancira.Application.UserSessions.Dtos;

public sealed record DeviceSessionsDto(
    string DeviceId,
    string? DeviceName,
    string? UserAgent,
    DateTimeOffset LastActivityAt,
    IReadOnlyList<UserSessionDto> Sessions);

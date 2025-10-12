using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avancira.Application.UserSessions.Dtos;

namespace Avancira.Application.UserSessions;

public interface IUserSessionService
{
    Task<UserSessionDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<UserSessionDto>> GetActiveByUserAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeviceSessionsDto>> GetActiveByUserGroupedByDeviceAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<UserSessionDto> CreateAsync(CreateUserSessionDto dto, CancellationToken cancellationToken = default);

    Task<UserSessionDto> UpdateActivityAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<bool> RevokeAsync(Guid sessionId, string? reason = null, CancellationToken cancellationToken = default);

    Task<int> RevokeAllAsync(string userId, Guid? excludeSessionId = null, CancellationToken cancellationToken = default);
}

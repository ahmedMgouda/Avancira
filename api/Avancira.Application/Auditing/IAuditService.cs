using Avancira.Domain.Auditing;

namespace Avancira.Application.Audit;
public interface IAuditService
{
    Task<List<AuditTrail>> GetUserTrailsAsync(Guid userId);
}

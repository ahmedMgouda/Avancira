using Avancira.Application.Audit;
using Avancira.Domain.Auditing;
using Avancira.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Avancira.Infrastructure.Identity.Audit;
public class AuditService(AvanciraDbContext context) : IAuditService
{
    public async Task<List<AuditTrail>> GetUserTrailsAsync(Guid userId)
    {
        var trails = await context.AuditTrails
           .Where(a => a.UserId == userId)
           .OrderByDescending(a => a.DateTime)
           .Take(250)
           .ToListAsync();
        return trails;
    }
}
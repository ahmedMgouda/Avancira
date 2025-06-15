using Avancira.Domain.Auditing;
using Avancira.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure.Identity.Audit;
public class AuditPublishedEventHandler: INotificationHandler<AuditPublishedEvent>
{

    private readonly ILogger<AuditPublishedEventHandler> _logger;
    private readonly IServiceProvider _serviceProvider;

    public AuditPublishedEventHandler(ILogger<AuditPublishedEventHandler> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }
    public async Task Handle(AuditPublishedEvent notification, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AvanciraDbContext>();

        _logger.LogInformation("received audit trails");

        try
        {
            await context.Set<AuditTrail>().AddRangeAsync(notification.Trails!, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            _logger.LogError("error while saving audit trail");
        }
        return;
    }
}
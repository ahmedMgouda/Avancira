using Avancira.Application.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Avancira.Infrastructure.Messaging;

public static class Extensions
{
    public static IServiceCollection ConfigureMessaging(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSignalR();

        // Register notification channels
        services.AddScoped<INotificationChannel, EmailNotificationChannel>();
        services.AddScoped<INotificationChannel, SignalRNotificationChannel>();

        return services;
    }
}

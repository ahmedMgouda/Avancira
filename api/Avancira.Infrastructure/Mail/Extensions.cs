using Avancira.Application.Mail;
using Microsoft.Extensions.DependencyInjection;

namespace Avancira.Infrastructure.Mail;
internal static class Extensions
{
    internal static IServiceCollection ConfigureMailing(this IServiceCollection services)
    {
        services.AddTransient<IMailService, SmtpMailService>();
        services.AddTransient<IEnhancedEmailService, EnhancedEmailService>();
        
        services.AddOptions<MailOptions>().BindConfiguration(nameof(MailOptions));
        services.AddOptions<EnhancedEmailOptions>().BindConfiguration(nameof(EnhancedEmailOptions));
        services.AddOptions<GraphApiOptions>().BindConfiguration(nameof(GraphApiOptions));
        services.AddOptions<SendGridOptions>().BindConfiguration(nameof(SendGridOptions));
        
        return services;
    }
}

using Avancira.Application.Mail;
using Avancira.Infrastructure.Common.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Avancira.Infrastructure.Mail;
internal static class Extensions
{
    internal static IServiceCollection ConfigureMailing(this IServiceCollection services)
    {
        services.AddTransient<IMailService, SmtpMailService>();
        services.AddTransient<IEnhancedEmailService, EnhancedEmailService>();
        
        services.AddOptions<MailOptions>().BindConfiguration(nameof(MailOptions));
        
        // Configure options with environment variable expansion
        services.AddOptions<EnhancedEmailOptions>()
            .Configure<IConfiguration>((options, config) =>
            {
                config.GetSection(nameof(EnhancedEmailOptions)).Bind(options);
                options.FromEmail = options.FromEmail.ExpandEnvironmentVariables();
                options.FromName = options.FromName.ExpandEnvironmentVariables();
            });
            
        services.AddOptions<GraphApiOptions>()
            .Configure<IConfiguration>((options, config) =>
            {
                config.GetSection(nameof(GraphApiOptions)).Bind(options);
                options.ClientId = options.ClientId.ExpandEnvironmentVariables();
                options.ClientSecret = options.ClientSecret.ExpandEnvironmentVariables();
                options.TenantId = options.TenantId.ExpandEnvironmentVariables();
            });
            
        services.AddOptions<SendGridOptions>()
            .Configure<IConfiguration>((options, config) =>
            {
                config.GetSection(nameof(SendGridOptions)).Bind(options);
                options.ApiKey = options.ApiKey.ExpandEnvironmentVariables();
            });
        
        return services;
    }
}

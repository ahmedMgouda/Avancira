using Avancira.Infrastructure.Constants;
using Avancira.Shared.Authorization;
using Hangfire.Client;
using Hangfire.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Infrastructure.Jobs;
public class AvanciraJobFilter : IClientFilter
{
    private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

    private readonly IServiceProvider _services;

    public AvanciraJobFilter(IServiceProvider services) => _services = services;

    public void OnCreating(CreatingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Logger.InfoFormat("Set UserId parameter to job {0}.{1}...", context.Job.Method.ReflectedType?.FullName, context.Job.Method.Name);

        using var scope = _services.CreateScope();

        var httpContext = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>()?.HttpContext;
        
        // For background/recurring jobs, HttpContext might not be available
        if (httpContext != null)
        {
            // Removed multi-tenancy logic (tenant info handling)
            string? userId = httpContext.User.GetUserId();
            context.SetJobParameter(QueryStringKeys.UserId, userId);
        }
        else
        {
            // For background jobs without HttpContext, we can either:
            // 1. Set a system user ID if needed
            // 2. Or just log that this is a system job
            Logger.InfoFormat("Creating system job {0}.{1} without HttpContext", 
                context.Job.Method.ReflectedType?.FullName, context.Job.Method.Name);
            
            // Optionally set a system user ID for background jobs
            // context.SetJobParameter(QueryStringKeys.UserId, "system");
        }
    }

    public void OnCreated(CreatedContext context) =>
        Logger.InfoFormat(
            "Job created with parameters {0}",
            context.Parameters.Select(x => x.Key + "=" + x.Value).Aggregate((s1, s2) => s1 + ";" + s2));
}

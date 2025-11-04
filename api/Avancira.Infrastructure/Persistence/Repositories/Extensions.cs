using Avancira.Application.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Avancira.Infrastructure.Persistence.Repositories;

public static class Extensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));

        return services;
    }
}

 
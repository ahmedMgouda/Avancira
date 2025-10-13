using Avancira.Application.Persistence;
using Avancira.Domain.Catalog;
using Avancira.Domain.UserSessions;
using Microsoft.Extensions.DependencyInjection;

namespace Avancira.Infrastructure.Persistence.Repositories;
public static class Extensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IRepository<Category>, CategoryRepository<Category>>();
        services.AddScoped<IReadRepository<Category>, CategoryRepository<Category>>();

        services.AddScoped<IRepository<UserSession>, SessionRepository<UserSession>>();
        services.AddScoped<IReadRepository<UserSession>, SessionRepository<UserSession>>();

        return services;
    }
}
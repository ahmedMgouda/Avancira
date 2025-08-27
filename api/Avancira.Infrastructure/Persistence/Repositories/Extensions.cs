using Avancira.Application.Persistence;
using Avancira.Domain.Catalog;
using Microsoft.Extensions.DependencyInjection;

namespace Avancira.Infrastructure.Persistence.Repositories;
public static class Extensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IRepository<Category>, CategoryRepository<Category>>();
        services.AddScoped<IReadRepository<Category>, CategoryRepository<Category>>();

        return services;
    }
}
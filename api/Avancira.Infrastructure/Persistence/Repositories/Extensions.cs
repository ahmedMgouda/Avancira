﻿using Avancira.Application.Persistence;
using Avancira.Domain.Catalog;
using Avancira.Domain.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Avancira.Infrastructure.Persistence.Repositories;
public static class Extensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IRepository<Category>, CategoryRepository<Category>>();
        services.AddScoped<IReadRepository<Category>, CategoryRepository<Category>>();
        services.AddScoped<IRepository<Chat>, ChatRepository<Chat>>();
        services.AddScoped<IReadRepository<Chat>, ChatRepository<Chat>>();
        return services;
    }
}

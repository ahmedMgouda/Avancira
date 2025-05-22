using Avancira.Application.Catalog.Categories;
using Avancira.Application.Catalog.Chats;
using Avancira.Application.Services.Category;
using Microsoft.Extensions.DependencyInjection;

namespace Avancira.Application;
public static class Extensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IChatService, ChatService>();

        return services;
    }
}
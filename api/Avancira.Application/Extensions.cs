using Avancira.Application.SubjectCategories;
using Avancira.Application.Subjects;
using Avancira.Application.UserSessions;
using Avancira.Application.UserSessions.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Avancira.Application;

public static class Extensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ISubjectCategoryService, SubjectCategoryService>();
        services.AddScoped<ISubjectService, SubjectService>();
        services.AddScoped<IUserSessionService, UserSessionService>();
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}

using Avancira.Application.Countries;
using Avancira.Application.LessonMaterials;
using Avancira.Application.Lessons;
using Avancira.Application.StudentReviews;
using Avancira.Application.StudentProfiles;
using Avancira.Application.SubjectCategories;
using Avancira.Application.Subjects;
using Avancira.Application.TutorProfiles;
using Avancira.Application.Listings;
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
        services.AddScoped<ICountryService, CountryService>();
        services.AddScoped<ISubjectCategoryService, SubjectCategoryService>();
        services.AddScoped<ISubjectService, SubjectService>();
        services.AddScoped<IListingService, ListingService>();
        services.AddScoped<ILessonService, LessonService>();
        services.AddScoped<IStudentProfileService, StudentProfileService>();
        services.AddScoped<ITutorProfileService, TutorProfileService>();
        services.AddScoped<IStudentReviewService, StudentReviewService>();
        services.AddScoped<ILessonMaterialService, LessonMaterialService>();
        services.AddScoped<IUserSessionService, UserSessionService>();
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}

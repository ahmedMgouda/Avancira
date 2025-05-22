using Avancira.Application.Audit;
using Avancira.Application.Identity.Roles;
using Avancira.Application.Identity.Tokens;
using Avancira.Application.Persistence;
using Avancira.Infrastructure.Auth;
using Avancira.Infrastructure.Identity.Audit;
using Avancira.Infrastructure.Identity.Roles;
using Avancira.Infrastructure.Identity.Tokens;
using Avancira.Infrastructure.Identity.Users.Services;
using Avancira.Infrastructure.Identity.Users;
using Avancira.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avancira.Application.Catalog;

namespace Avancira.Infrastructure.Catalog
{
    internal static class Extensions
    {
        internal static IServiceCollection ConfigureCatalog(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddTransient<ILessonCategoryService, LessonCategoryService>();
            services.AddTransient<IListingService, ListingService>();
            services.AddTransient<IUserService, UserService>();
            services.AddTransient<IChatService, ChatService>();

            return services;
        }
    }
}

using Avancira.Application.Audit;
using Avancira.Application.Billing;
using Avancira.Application.Identity.Roles;
using Avancira.Application.Persistence;
using Avancira.Application.Identity.Users.Abstractions;
using Avancira.Application.Messaging;
using Avancira.Application.Payments;
using Avancira.Application.Subscriptions;
using Avancira.Application.Common;
using Avancira.Infrastructure.Billing;
using Avancira.Infrastructure.Identity.Audit;
using Avancira.Infrastructure.Identity.Roles;
using Avancira.Infrastructure.Identity.Users.Services;
using Avancira.Infrastructure.Identity.Users;
using Avancira.Infrastructure.Persistence;
using Avancira.Infrastructure.Common;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avancira.Infrastructure.Payments;
using UAParser;
using Avancira.Application.SubjectCategories;
using Avancira.Application.Subjects;

namespace Avancira.Infrastructure.Catalog
{
    internal static class Extensions
    {
        internal static IServiceCollection ConfigureCatalog(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddTransient<IPayPalAccountService, PayPalAccountService>();
            services.AddTransient<IStripeAccountService, StripeAccountService>();
            services.AddTransient<IStripeCardService, StripeCardService>();
            services.AddTransient<IPaymentService, PaymentService>();
            services.AddTransient<IChatService, ChatService>();
            services.AddTransient<ISubjectCategoryService, SubjectCategoryService>();
            services.AddTransient<ISubjectService, SubjectService>();
            services.AddTransient<ISubscriptionService, SubscriptionService>();
            services.AddTransient<INotificationService, NotificationService>();
            services.AddTransient<IWalletService, WalletService>();
            services.AddHttpClient();
            services.AddSingleton<Parser>(_ => Parser.GetDefault());

            // Register billing services
            services.AddTransient<IPaymentGateway, DefaultPaymentGateway>();
            services.AddTransient<IPaymentGatewayFactory, PaymentGatewayFactory>();

            return services;
        }
    }
}

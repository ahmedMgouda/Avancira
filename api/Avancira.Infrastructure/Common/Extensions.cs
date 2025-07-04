﻿using Avancira.Application.Audit;
using Avancira.Application.Billing;
using Avancira.Application.Identity.Roles;
using Avancira.Application.Identity.Tokens;
using Avancira.Application.Persistence;
using Avancira.Application.Catalog;
using Avancira.Application.Identity.Users.Abstractions;
using Avancira.Application.Lessons;
using Avancira.Application.Messaging;
using Avancira.Application.Payments;
using Avancira.Application.Subscriptions;
using Avancira.Infrastructure.Auth;
using Avancira.Infrastructure.Billing;
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
using Avancira.Application.Categories;
using Avancira.Infrastructure.Payments;

namespace Avancira.Infrastructure.Catalog
{
    internal static class Extensions
    {
        internal static IServiceCollection ConfigureCatalog(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddTransient<ILessonCategoryService, LessonCategoryService>();
            services.AddTransient<IListingService, ListingService>();
            services.AddTransient<IPayPalAccountService, PayPalAccountService>();
            services.AddTransient<IStripeAccountService, StripeAccountService>();
            services.AddTransient<IStripeCardService, StripeCardService>();
            services.AddTransient<IPaymentService, PaymentService>();
            services.AddTransient<IEvaluationService, EvaluationService>();
            services.AddTransient<IChatService, ChatService>();
            services.AddTransient<ICategoryService, CategoryService>();
            services.AddTransient<ISubscriptionService, SubscriptionService>();
            services.AddTransient<INotificationService, NotificationService>();
            services.AddTransient<IWalletService, WalletService>();
            services.AddTransient<ILessonService, LessonService>();
            services.AddTransient<IJwtTokenService, JwtTokenService>();
            services.AddTransient<IGeolocationService, GeolocationService>();
            services.AddTransient<IFileUploadService, FileUploadService>();
            
            // Register billing services
            services.AddTransient<IPaymentGateway, DefaultPaymentGateway>();
            services.AddTransient<IPaymentGatewayFactory, PaymentGatewayFactory>();

            return services;
        }
    }
}

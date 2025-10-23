using Avancira.Application.Billing;
using Avancira.Application.Catalog;
using Avancira.Application.Catalog.Dtos;
using Avancira.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Avancira.Infrastructure.Catalog
{
    public class StripeCardService : IStripeCardService
    {
        private readonly AppOptions _appOptions;
        private readonly StripeOptions _stripeOptions;
        private readonly AvanciraDbContext _dbContext;
        private readonly ILogger<StripeCardService> _logger;

        public StripeCardService(
            IOptions<AppOptions> appOptions,
            IOptions<StripeOptions> stripeOptions,
            AvanciraDbContext dbContext,
            ILogger<StripeCardService> logger
        )
        {
            _appOptions = appOptions.Value;
            _stripeOptions = stripeOptions.Value;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> AddUserCardAsync(string userId, SaveCardDto request)
        {
            StripeConfiguration.ApiKey = _stripeOptions.ApiKey;

            var user = await _dbContext.Users.AsTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) throw new KeyNotFoundException("User not found.");

            _logger.LogWarning("Stripe integration is not configured for user {UserId}.", userId);
            throw new InvalidOperationException("Stripe integration is not configured for this user.");
        }

        // TODO : Pagination
        public async Task<IEnumerable<CardDto>> GetUserCardsAsync(string userId)
        {
            return await _dbContext.UserCards
                .Where(c => c.UserId == userId)
                .Select(c => new CardDto
                {
                    Id = c.Id,
                    Last4 = c.Last4,
                    ExpMonth = c.ExpMonth,
                    ExpYear = c.ExpYear,
                    Type = c.Brand,
                    Purpose = c.Type
                })
                .ToListAsync();
        }

        public async Task<bool> RemoveUserCardAsync(string userId, int cardId)
        {
            StripeConfiguration.ApiKey = _stripeOptions.ApiKey;

            var user = await _dbContext.Users.AsTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            _logger.LogWarning("Stripe integration is not configured for user {UserId}.", userId);
            throw new InvalidOperationException("Stripe integration is not configured for this user.");
        }
    }
}

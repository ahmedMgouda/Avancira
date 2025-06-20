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

            if (string.IsNullOrEmpty(user.StripeCustomerId))
            {
                var customerService = new CustomerService();
                var customer = await customerService.CreateAsync(new CustomerCreateOptions
                {
                    Email = user.Email,
                    Name = $"{user.FirstName} {user.LastName}".Trim(),
                });

                user.StripeCustomerId = customer.Id;
                _dbContext.Users.Update(user);
                await _dbContext.SaveChangesAsync();
            }

            var cardService = new CardService();
            var card = await cardService.CreateAsync(user.StripeCustomerId, new CardCreateOptions
            {
                Source = request.StripeToken,
            });

            var userCard = new Domain.UserCard.UserCard
            {
                UserId = userId,
                CardId = card.Id,
                Last4 = card.Last4,
                ExpMonth = card.ExpMonth,
                ExpYear = card.ExpYear,
                Brand = card.Brand ?? "unknown",
                Type = request.Purpose,
            };

            _dbContext.UserCards.Add(userCard);
            await _dbContext.SaveChangesAsync();

            return true;
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
            if (user == null || string.IsNullOrEmpty(user.StripeCustomerId))
            {
                throw new KeyNotFoundException("User or Stripe customer not found.");
            }

            var card = await _dbContext.UserCards.FirstOrDefaultAsync(c => c.Id == cardId && c.UserId == userId);
            if (card == null)
            {
                throw new KeyNotFoundException("Card not found.");
            }

            var cardService = new CardService();
            await cardService.DeleteAsync(user.StripeCustomerId, card.CardId);

            _dbContext.UserCards.Remove(card);
            await _dbContext.SaveChangesAsync();

            // Check if there are any remaining cards for the user
            var remainingCards = _dbContext.UserCards.Any(c => c.UserId == userId);
            if (!remainingCards)
            {
                // If no cards remain, clear the StripeCustomerId
                user.StripeCustomerId = null;
                _dbContext.Users.Update(user);
                await _dbContext.SaveChangesAsync();
            }

            return true;
        }
    }
}

using Avancira.Application.Catalog;
using Avancira.Application.Catalog.Dtos;
using Avancira.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Avancira.Infrastructure.Catalog
{
    public class StripeCardService : IStripeCardService
    {
        private readonly AvanciraDbContext _dbContext;
        private readonly ILogger<StripeCardService> _logger;

        public StripeCardService(
            AvanciraDbContext dbContext,
            ILogger<StripeCardService> logger
        )
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> AddUserCardAsync(string userId, SaveCardDto request)
        {
            try
            {
                // Validate input
                if (string.IsNullOrEmpty(userId))
                    throw new ArgumentException("User ID is required");

                if (request == null)
                    throw new ArgumentException("Card request is required");

                // Check if user exists
                var user = await _dbContext.Users.FindAsync(userId);
                if (user == null)
                    throw new KeyNotFoundException("User not found");

                // In a real implementation, you would:
                // 1. Create a payment method in Stripe
                // 2. Attach it to the customer
                // 3. Store the payment method ID in your database

                // For now, we'll create a placeholder card record
                var card = new Domain.UserCard.UserCard
                {
                    UserId = userId,
                    CardId = request.StripeToken,
                    Last4 = "0000", // In real implementation, get from Stripe
                    Brand = "visa", // In real implementation, get from Stripe
                    ExpMonth = 12, // In real implementation, get from Stripe
                    ExpYear = DateTime.Now.Year + 1, // In real implementation, get from Stripe
                    Type = request.Purpose
                };

                await _dbContext.UserCards.AddAsync(card);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Card added successfully for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding card for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<CardDto>> GetUserCardsAsync(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    throw new ArgumentException("User ID is required");

                var cards = await _dbContext.UserCards
                    .Where(c => c.UserId == userId)
                    .OrderBy(c => c.Id)
                    .Select(c => new CardDto
                    {
                        Id = c.Id,
                        Last4 = c.Last4,
                        Type = c.Brand ?? "visa",
                        ExpMonth = c.ExpMonth,
                        ExpYear = c.ExpYear,
                        Purpose = c.Type
                    })
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} cards for user {UserId}", cards.Count, userId);
                return cards;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cards for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> RemoveUserCardAsync(string userId, int cardId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    throw new ArgumentException("User ID is required");

                // Find the card by ID
                var card = await _dbContext.UserCards
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.Id == cardId);

                if (card == null)
                {
                    _logger.LogWarning("Card with ID {CardId} not found for user {UserId}", cardId, userId);
                    return false;
                }

                // In a real implementation, you would also:
                // 1. Detach the payment method from Stripe
                // 2. Delete it from Stripe if needed

                _dbContext.UserCards.Remove(card);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Card removed successfully for user {UserId}, card ID {CardId}", userId, cardId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing card {CardId} for user {UserId}", cardId, userId);
                throw;
            }
        }
    }
}

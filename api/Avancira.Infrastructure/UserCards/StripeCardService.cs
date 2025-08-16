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

            // Start database transaction
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            
            try
            {
                var user = await _dbContext.Users.AsTracking().FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null) throw new KeyNotFoundException("User not found.");

                string stripeCustomerId = user.StripeCustomerId;
                bool customerCreated = false;
                var customerService = new CustomerService();

                // Handle existing or create new Stripe customer
                if (string.IsNullOrEmpty(stripeCustomerId))
                {
                    // Create new Stripe customer
                    var customer = await customerService.CreateAsync(new CustomerCreateOptions
                    {
                        Email = user.Email,
                        Name = $"{user.FirstName} {user.LastName}".Trim(),
                    });

                    stripeCustomerId = customer.Id;
                    customerCreated = true;
                    
                    _logger.LogInformation("Created new Stripe customer {CustomerId} for user {UserId}", stripeCustomerId, userId);
                }
                else
                {
                    // Verify existing customer still exists in Stripe
                    try
                    {
                        await customerService.GetAsync(stripeCustomerId);
                        _logger.LogDebug("Reusing existing Stripe customer {CustomerId} for user {UserId}", stripeCustomerId, userId);
                    }
                    catch (StripeException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        // Customer was deleted from Stripe, create a new one
                        var customer = await customerService.CreateAsync(new CustomerCreateOptions
                        {
                            Email = user.Email,
                            Name = $"{user.FirstName} {user.LastName}".Trim(),
                        });

                        stripeCustomerId = customer.Id;
                        customerCreated = true;
                        
                        _logger.LogWarning("Stripe customer {OldCustomerId} not found, created new customer {NewCustomerId} for user {UserId}", 
                            user.StripeCustomerId, stripeCustomerId, userId);
                    }
                }

                // Create card in Stripe (this is where the token validation happens)
                var cardService = new CardService();
                var card = await cardService.CreateAsync(stripeCustomerId, new CardCreateOptions
                {
                    Source = request.StripeToken,
                });

                // If we reach here, both Stripe operations succeeded
                // Now update the database atomically
                if (customerCreated)
                {
                    user.StripeCustomerId = stripeCustomerId;
                    _dbContext.Users.Update(user);
                }

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
                
                // Commit the transaction
                await transaction.CommitAsync();
                
                _logger.LogInformation("Successfully added card for user {UserId}", userId);
                return true;
            }
            catch (StripeException stripeEx)
            {
                // Rollback database transaction
                await transaction.RollbackAsync();
                
                _logger.LogError(stripeEx, "Stripe error while adding card for user {UserId}: {Error}", userId, stripeEx.Message);
                throw new InvalidOperationException($"Payment card error: {stripeEx.Message}", stripeEx);
            }
            catch (Exception ex)
            {
                // Rollback database transaction
                await transaction.RollbackAsync();
                
                _logger.LogError(ex, "Error adding card for user {UserId}", userId);
                throw;
            }
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

            // Start database transaction
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            
            try
            {
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

                // Delete card from Stripe first
                var cardService = new CardService();
                await cardService.DeleteAsync(user.StripeCustomerId, card.CardId);

                // If Stripe deletion succeeded, update database
                _dbContext.UserCards.Remove(card);

                // Check if there are any remaining cards for the user
                var remainingCards = await _dbContext.UserCards.CountAsync(c => c.UserId == userId && c.Id != cardId);
                if (remainingCards == 0)
                {
                    // If no cards remain, clear the StripeCustomerId
                    user.StripeCustomerId = null;
                    _dbContext.Users.Update(user);
                }

                await _dbContext.SaveChangesAsync();
                
                // Commit the transaction
                await transaction.CommitAsync();
                
                _logger.LogInformation("Successfully removed card {CardId} for user {UserId}", cardId, userId);
                return true;
            }
            catch (StripeException stripeEx)
            {
                // Rollback database transaction
                await transaction.RollbackAsync();
                
                _logger.LogError(stripeEx, "Stripe error while removing card {CardId} for user {UserId}: {Error}", cardId, userId, stripeEx.Message);
                throw new InvalidOperationException($"Payment card removal error: {stripeEx.Message}", stripeEx);
            }
            catch (Exception ex)
            {
                // Rollback database transaction
                await transaction.RollbackAsync();
                
                _logger.LogError(ex, "Error removing card {CardId} for user {UserId}", cardId, userId);
                throw;
            }
        }
    }
}

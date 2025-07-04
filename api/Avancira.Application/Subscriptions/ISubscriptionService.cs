using System.Threading.Tasks;
using Avancira.Application.Catalog.Dtos;
using Avancira.Domain.Catalog.Enums;
using Avancira.Domain.Subscription;
using Avancira.Domain.PromoCodes;
using Avancira.Application.Lessons.Dtos;

namespace Avancira.Application.Subscriptions;

public interface ISubscriptionService
{
    // Create
    Task<(int SubscriptionId, Guid TransactionId)> CreateSubscriptionAsync(SubscriptionRequestDto request, string userId);

    // Read
    Task<bool> HasActiveSubscriptionAsync(string userId);
    Task<PagedResult<Subscription>> ListUserSubscriptionsAsync(string userId, int page, int pageSize);
    Task<PromoCode> ValidatePromoCode(string promoCode);
    Task<SubscriptionDetailsDto?> FetchSubscriptionDetailsAsync(string userId);

    // Update
    Task<bool> ChangeBillingFrequencyAsync(string userId, SubscriptionBillingFrequency newFrequency);

    // Delete
    Task<bool> CancelSubscriptionAsync(string userId);
}

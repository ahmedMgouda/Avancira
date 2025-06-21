using System;
using System.Threading.Tasks;
using Avancira.Application.Catalog;
using Avancira.Application.Catalog.Dtos;
using Avancira.Application.Subscriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[Route("api/subscriptions")]
public class SubscriptionsController : BaseApiController
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsController(
        ISubscriptionService subscriptionService
    )
    {
        _subscriptionService = subscriptionService;
    }

    // Create
    [Authorize]
    [HttpPost("create")]
    public async Task<IActionResult> CreateSubscription([FromBody] SubscriptionRequestDto request)
    {
        try
        {
            var userId = GetUserId();
            var (subscriptionId, transactionId) = await _subscriptionService.CreateSubscriptionAsync(request, userId);
            return Ok(new { SubscriptionId = subscriptionId, TransactionId = transactionId });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Message = "An error occurred while creating the subscription.",
                Error = ex.Message
            });
        }
    }

    // Read
    [Authorize]
    [HttpGet("check-active")]
    public async Task<IActionResult> CheckActiveSubscription()
    {
        var userId = GetUserId();
        var hasActiveSubscription = await _subscriptionService.HasActiveSubscriptionAsync(userId);
        return Ok(new { IsActive = hasActiveSubscription });
    }

    [Authorize]
    [HttpGet("")]
    public async Task<IActionResult> GetUserSubscriptions([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = GetUserId();
        var subscriptions = await _subscriptionService.ListUserSubscriptionsAsync(userId, page, pageSize);

        if (subscriptions == null || subscriptions.TotalResults == 0)
            return NotFound("No subscriptions found.");

        return Ok(subscriptions);
    }

    [Authorize]
    [HttpGet("validate-promo")]
    public async Task<IActionResult> ValidatePromoCode([FromQuery] string promoCode)
    {
        var promo = await _subscriptionService.ValidatePromoCode(promoCode);

        if (promo == null)
        {
            return BadRequest(new { Message = "Invalid or expired promo code." });
        }

        return Ok(new
        {
            PromoCode = promo.Code,
            DiscountAmount = promo.DiscountAmount,
            DiscountPercentage = promo.DiscountPercentage
        });
    }

    [Authorize]
    [HttpGet("details")]
    public async Task<IActionResult> GetSubscriptionDetails()
    {
        var userId = GetUserId();
        var details = await _subscriptionService.FetchSubscriptionDetailsAsync(userId);
        if (details == null) return NotFound(new { Message = "No active subscription found." });

        return Ok(details);
    }

    // Update
    [Authorize]
    [HttpPut("change-frequency")]
    public async Task<IActionResult> ChangeBillingFrequency([FromBody] ChangeFrequencyRequestDto request)
    {
        var userId = GetUserId();
        var success = await _subscriptionService.ChangeBillingFrequencyAsync(userId, request.NewFrequency);

        if (!success)
            return BadRequest(new { Message = "Failed to change billing frequency." });

        return Ok(new { Message = "Billing frequency updated successfully." });
    }

    // Delete
    [Authorize]
    [HttpDelete("cancel")]
    public async Task<IActionResult> CancelSubscription()
    {
        var userId = GetUserId();
        var success = await _subscriptionService.CancelSubscriptionAsync(userId);

        if (!success)
            return BadRequest(new { Message = "Failed to cancel subscription." });

        return Ok(new { Message = "Subscription cancelled successfully." });
    }
}

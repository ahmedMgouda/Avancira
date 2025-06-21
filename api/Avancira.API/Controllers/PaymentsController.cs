using System;
using System.Threading.Tasks;
using Avancira.Application.Catalog;
using Avancira.Application.Catalog.Dtos;
using Avancira.Application.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[Route("api/payments")]
public class PaymentsController : BaseApiController
{
    private readonly IPayPalAccountService _payPalAccountService;
    private readonly IStripeAccountService _stripeAccountService;
    private readonly IStripeCardService _stripeCardService;
    private readonly IPaymentService _paymentService;

    public PaymentsController(
        IPayPalAccountService payPalAccountService,
        IStripeAccountService stripeAccountService,
        IStripeCardService stripeCardService,
        IPaymentService paymentService
    )
    {
        _payPalAccountService = payPalAccountService;
        _stripeAccountService = stripeAccountService;
        _stripeCardService = stripeCardService;
        _paymentService = paymentService;
    }

    [HttpPost("create-payment")]
    public async Task<IActionResult> CreatePayment([FromBody] PaymentRequestDto request)
    {
        try
        {
            var paymentResult = await _paymentService.CreatePaymentAsync(request);

        return Ok(new
        {
            success = true,
            paymentId = paymentResult.PaymentId,
            approvalUrl = paymentResult.ApprovalUrl
        });
    }
    catch (Exception ex)
    {
        return BadRequest(ex.Message);
        }
    }

    [Authorize]
    [HttpPost("create-payout")]
    public async Task<IActionResult> CreatePayout([FromBody] CreatePayoutRequest request)
    {
        // TODO: Implement proper user ID extraction from claims
        // var userId = User.GetUserId();
        var userId = "temp-user-id"; // Temporary placeholder
        var userPaymentGateway = "Stripe"; // TODO: Get from user service
        try
        {
            var payoutId = await _paymentService.CreatePayoutAsync(userId, request.Amount, request.Currency.ToLower(), userPaymentGateway);

            return Ok(new
            {
                success = true,
                message = "Payout processed successfully",
                payoutId
            });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("capture-payment")]
    public async Task<IActionResult> CapturePayment([FromBody] CapturePaymentRequestDto request)
    {
        try
        {
        // TODO: Implement proper user ID extraction from claims
        // var userId = User.GetUserId();
        var userId = "temp-user-id"; // Temporary placeholder
        var result = await _paymentService.CapturePaymentAsync(request.TransactionId, request.PaymentMethod.ToString());

        if (result != null)
        {
            // Return success response
            return Ok(new
            {
                success = true,
                message = "Payment captured and subscription created successfully.",
            });
        }

        return BadRequest("Failed to capture payment.");
    }
    catch (Exception ex)
    {
        return BadRequest(ex.Message);
        }
    }

    [Authorize]
    [HttpGet("history")]
    public async Task<IActionResult> HistoryAsync()
    {
        // TODO: Implement proper user ID extraction from claims
        // var userId = User.GetUserId();
        var userId = "temp-user-id"; // Temporary placeholder
        var paymentHistory = await _paymentService.GetPaymentHistoryAsync(userId);
        return Ok(paymentHistory);
    }

    #region Stripe Cards
    [Authorize]
    [HttpPost("save-card")]
    public async Task<IActionResult> SaveCard([FromBody] SaveCardDto request)
    {
        // TODO: Implement proper user ID extraction from claims
        // var userId = User.GetUserId();
        var userId = "temp-user-id"; // Temporary placeholder
        await _stripeCardService.AddUserCardAsync(userId, request);
        return Ok(new { success = true, message = "Card saved successfully." });
    }

    [Authorize]
    [HttpDelete("remove-card/{Id}")]
    public async Task<IActionResult> RemoveCard(int Id)
    {
        // TODO: Implement proper user ID extraction from claims
        // var userId = User.GetUserId();
        var userId = "temp-user-id"; // Temporary placeholder
        await _stripeCardService.RemoveUserCardAsync(userId, Id);
        return Ok(new { success = true, message = "Card removed successfully." });
    }

    [Authorize]
    [HttpGet("saved-cards")]
    public async Task<IActionResult> GetSavedCardsAsync()
    {
        // TODO: Implement proper user ID extraction from claims
        // var userId = User.GetUserId();
        var userId = "temp-user-id"; // Temporary placeholder
        var cards = await _stripeCardService.GetUserCardsAsync(userId);
        return Ok(cards);
    }
    #endregion

    #region Stripe Accounts
    [Authorize]
    [HttpGet("connect-stripe-account")]
    public async Task<IActionResult> CreateStripeAccount()
    {
        // TODO: Implement proper user ID extraction from claims
        // var userId = User.GetUserId();
        var userId = "temp-user-id"; // Temporary placeholder

        var url = await _stripeAccountService.ConnectStripeAccountAsync(userId);

        return Ok(new { url });
    }
    #endregion

    #region PayPal Accounts
    [Authorize]
    [HttpPost("connect-paypal-account")]
    public async Task<IActionResult> CreatePayPalAccount([FromBody] PayPalAuthRequest request)
    {
        // TODO: Implement proper user ID extraction from claims
        // var userId = User.GetUserId();
        var userId = "temp-user-id"; // Temporary placeholder
        try
        {
            var success = await _payPalAccountService.ConnectPayPalAccountAsync(userId, request.AuthCode);
            return Ok(new { success });
        }
        catch (Exception ex)
        {
            return Redirect(ex.Message);
        }
    }
    #endregion
}

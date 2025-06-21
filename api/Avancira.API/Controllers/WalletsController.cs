using System;
using System.Threading.Tasks;
using Avancira.Application.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[Route("api/wallets")]
public class WalletsController : BaseApiController
{
    private readonly IWalletService _walletService;

    public WalletsController(
        IWalletService walletService
    )
    {
        _walletService = walletService;
    }

    // Create
    //[Authorize]
    //[HttpPost("add-money")]
    //public async Task<IActionResult> AddMoneyToWallet([FromBody] PaymentRequestDto request)
    //{
    //    var userId = GetUserId();

    //    try
    //    {
    //        var result = await _walletService.AddMoneyToWallet(userId, request);
    //        return JsonOk(new { result.PayPalPaymentId, result.ApprovalUrl, result.TransactionId });
    //    }
    //    catch (Exception ex)
    //    {
    //        return JsonError("Failed to process payment.", ex.Message);
    //    }
    //}

    // Read
    [Authorize]
    [HttpGet("balance")]
    public async Task<IActionResult> GetWalletBalance()
    {
        // TODO: Implement proper user ID extraction from claims
        // var userId = User.GetUserId();
        var userId = "temp-user-id"; // Temporary placeholder

        try
        {
            var result = await _walletService.GetWalletBalanceAsync(userId);
            return Ok(new { Balance = result.Balance, LastUpdated = result.LastUpdated });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = "Failed to fetch wallet balance.", Error = ex.Message });
        }
    }
}

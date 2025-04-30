using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/wallets")]
[ApiController]
public class WalletsAPIController : BaseController
{
    private readonly IWalletService _walletService;

    public WalletsAPIController(
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
        var userId = GetUserId();

        try
        {
            var result = await _walletService.GetWalletBalanceAsync(userId);
            return JsonOk(new { Balance = result.Balance, LastUpdated = result.LastUpdated });
        }
        catch (Exception ex)
        {
            return JsonError("Failed to fetch wallet balance.", ex.Message);
        }
    }
}



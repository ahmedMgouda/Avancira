using System;
using System.Threading.Tasks;
using Avancira.Application.Catalog;
using Avancira.Application.Identity.Users.Abstractions;
using Avancira.Application.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[Route("api/wallets")]
public class WalletsController : BaseApiController
{
    private readonly IWalletService _walletService;
    private readonly ICurrentUser _currentUser;

    public WalletsController(
        IWalletService walletService,
        ICurrentUser currentUser
    )
    {
        _walletService = walletService;
        _currentUser = currentUser;
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
        try
        {
            var userId = _currentUser.GetUserId().ToString();
            var result = await _walletService.GetWalletBalanceAsync(userId);
            return Ok(new { Balance = result.Balance, LastUpdated = result.LastUpdated });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = "Failed to fetch wallet balance.", Error = ex.Message });
        }
    }
}

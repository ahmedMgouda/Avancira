using System;
using System.ComponentModel.DataAnnotations;
using Avancira.Application.Identity.Users.Abstractions;
using Avancira.Application.Identity.Users.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

namespace Avancira.API.Pages.Account;

[ValidateAntiForgeryToken]
public class ForgotPasswordModel : PageModel
{
    private readonly IUserService _userService;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan ResetCooldown = TimeSpan.FromSeconds(30);

    public ForgotPasswordModel(IUserService userService, IMemoryCache cache)
    {
        _userService = userService;
        _cache = cache;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? Message { get; set; }

    public int RemainingCooldown { get; private set; }

    public class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var cacheKey = $"pwreset:{Input.Email}:{ip}";
        var now = DateTimeOffset.UtcNow;

        if (_cache.TryGetValue(cacheKey, out DateTimeOffset lastRequest))
        {
            var elapsed = now - lastRequest;
            if (elapsed < ResetCooldown)
            {
                RemainingCooldown = (int)Math.Ceiling((ResetCooldown - elapsed).TotalSeconds);
                ModelState.AddModelError(string.Empty, $"Please wait {RemainingCooldown} seconds before requesting another password reset.");
                return Page();
            }
        }

        await _userService.ForgotPasswordAsync(new ForgotPasswordDto { Email = Input.Email }, HttpContext.RequestAborted);
        _cache.Set(cacheKey, now, ResetCooldown);
        RemainingCooldown = (int)ResetCooldown.TotalSeconds;
        Message = "If an account with that email exists, a password reset link has been sent.";
        return Page();
    }
}

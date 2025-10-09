using System.Security.Claims;
using Avancira.API.Models.Account;
using Avancira.Application.Identity.Users.Abstractions;
using Avancira.Application.Identity.Users.Dtos;
using Avancira.Domain.Common.Exceptions;
using Avancira.Infrastructure.Identity.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using OpenIddict.Abstractions;

namespace Avancira.API.Controllers;

[AllowAnonymous]
[AutoValidateAntiforgeryToken]
[Route("account")]
[Produces("text/html")]
public class AccountController : Controller
{
    private readonly SignInManager<User> _signInManager;
    private readonly IUserService _userService;
    private readonly IMemoryCache _cache;

    private static readonly HashSet<string> AllowedProviders =
        new(StringComparer.OrdinalIgnoreCase)
        {
            GoogleDefaults.AuthenticationScheme,
            FacebookDefaults.AuthenticationScheme
        };

    private static readonly TimeSpan ResetCooldown = TimeSpan.FromSeconds(30);

    public AccountController(
        SignInManager<User> signInManager,
        IUserService userService,
        IMemoryCache cache)
    {
        _signInManager = signInManager;
        _userService = userService;
        _cache = cache;
    }

    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null, string? provider = null)
    {
        var targetUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;

        if (!string.IsNullOrWhiteSpace(provider) && AllowedProviders.Contains(provider))
        {
            return Redirect($"/api/auth/external-login?provider={provider}&returnUrl={targetUrl}");
        }

        var model = new LoginViewModel { ReturnUrl = targetUrl };
        ViewData["Title"] = "Login";
        return View(model);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        var returnUrl = string.IsNullOrEmpty(model.ReturnUrl) ? "/connect/authorize" : model.ReturnUrl;

        if (!ModelState.IsValid)
        {
            model.ReturnUrl = returnUrl;
            ViewData["Title"] = "Login";
            return View(model);
        }

        var user = await _signInManager.UserManager.FindByEmailAsync(model.Email);
        if (user is null || !await _signInManager.UserManager.CheckPasswordAsync(user, model.Password))
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            model.ReturnUrl = returnUrl;
            ViewData["Title"] = "Login";
            return View(model);
        }

        var principal = await _signInManager.CreateUserPrincipalAsync(user);
        var identity = (ClaimsIdentity)principal.Identity!;

        AddClaimIfMissing(identity, OpenIddictConstants.Claims.Subject, user.Id);
        AddClaimIfMissing(identity, OpenIddictConstants.Claims.Name, user.UserName);
        AddClaimIfMissing(identity, OpenIddictConstants.Claims.GivenName, user.FirstName);
        AddClaimIfMissing(identity, OpenIddictConstants.Claims.FamilyName, user.LastName);
        AddClaimIfMissing(identity, OpenIddictConstants.Claims.Email, user.Email);
        AddClaimIfMissing(identity, OpenIddictConstants.Claims.EmailVerified, "true", ClaimValueTypes.Boolean);

        var roles = await _signInManager.UserManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            AddClaimIfMissing(identity, OpenIddictConstants.Claims.Role, role, matchValue: true);
        }

        await HttpContext.SignInAsync(
            IdentityConstants.ApplicationScheme,
            principal);

        return LocalRedirect(returnUrl);
    }

    [HttpGet("register")]
    public IActionResult Register(string? returnUrl = null)
    {
        var model = new RegisterViewModel { ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl };
        ViewData["Title"] = "Register";
        return View(model);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        var returnUrl = string.IsNullOrEmpty(model.ReturnUrl) ? "/connect/authorize" : model.ReturnUrl;

        if (!ModelState.IsValid)
        {
            model.ReturnUrl = returnUrl;
            ViewData["Title"] = "Register";
            return View(model);
        }

        var dto = new RegisterUserDto
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            UserName = model.UserName,
            Password = model.Password,
            ConfirmPassword = model.ConfirmPassword,
            AcceptTerms = model.AcceptTerms
        };

        var origin = $"{Request.Scheme}://{Request.Host.Value}{Request.PathBase.Value}";
        try
        {
            await _userService.RegisterAsync(dto, origin, HttpContext.RequestAborted);
            return RedirectToAction(nameof(Login), new { returnUrl });
        }
        catch (AvanciraException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.ReturnUrl = returnUrl;
            ViewData["Title"] = "Register";
            return View(model);
        }
    }

    [HttpGet("forgot-password")]
    public IActionResult ForgotPassword()
    {
        ViewData["Title"] = "Forgot Password";
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Forgot Password";
            return View(model);
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var cacheKey = $"pwreset:{model.Email}:{ip}";
        var now = DateTimeOffset.UtcNow;

        if (_cache.TryGetValue(cacheKey, out DateTimeOffset lastRequest))
        {
            var elapsed = now - lastRequest;
            if (elapsed < ResetCooldown)
            {
                model.RemainingCooldown = (int)Math.Ceiling((ResetCooldown - elapsed).TotalSeconds);
                ModelState.AddModelError(string.Empty, $"Please wait {model.RemainingCooldown} seconds before requesting another password reset.");
                ViewData["Title"] = "Forgot Password";
                return View(model);
            }
        }

        await _userService.ForgotPasswordAsync(new ForgotPasswordDto { Email = model.Email }, HttpContext.RequestAborted);
        _cache.Set(cacheKey, now, ResetCooldown);
        model.RemainingCooldown = (int)ResetCooldown.TotalSeconds;
        model.Message = "If an account with that email exists, a password reset link has been sent.";
        ViewData["Title"] = "Forgot Password";
        return View(model);
    }

    [HttpGet("reset-password")]
    public IActionResult ResetPassword(string? userId, string? token)
    {
        var model = new ResetPasswordViewModel
        {
            UserId = userId ?? string.Empty,
            Token = token ?? string.Empty
        };

        ViewData["Title"] = "Reset Password";
        return View(model);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Reset Password";
            return View(model);
        }

        var dto = new ResetPasswordDto
        {
            UserId = model.UserId,
            Password = model.Password,
            ConfirmPassword = model.ConfirmPassword,
            Token = model.Token
        };

        try
        {
            await _userService.ResetPasswordAsync(dto, HttpContext.RequestAborted);
            TempData["SuccessMessage"] = "Password reset successfully.";
            return RedirectToAction(nameof(Login));
        }
        catch (AvanciraException ex)
        {
            var errors = ex.ErrorMessages.Any() ? ex.ErrorMessages : new[] { ex.Message };
            foreach (var error in errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            ViewData["Title"] = "Reset Password";
            return View(model);
        }
    }

    private static void AddClaimIfMissing(
        ClaimsIdentity identity,
        string type,
        string? value,
        string? valueType = null,
        bool matchValue = false)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        bool hasClaim = matchValue
            ? identity.HasClaim(c => c.Type == type && c.Value == value)
            : identity.HasClaim(c => c.Type == type);

        if (!hasClaim)
        {
            identity.AddClaim(valueType is null
                ? new Claim(type, value)
                : new Claim(type, value, valueType));
        }
    }
}

using System.Security.Claims;
using Avancira.Auth.Helpers;
using Avancira.Auth.Models.Account;
using Avancira.Application.Identity.Users.Abstractions;
using Avancira.Application.Identity.Users.Dtos;
using Avancira.Domain.Common.Exceptions;
using Avancira.Infrastructure.Identity.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using Avancira.Shared.Authorization;
using IdentityConstants = Microsoft.AspNetCore.Identity.IdentityConstants;

namespace Avancira.Auth.Controllers;

[AllowAnonymous]
[AutoValidateAntiforgeryToken]
[Route("account")]
[Produces("text/html")]
public class AccountController : Controller
{
    private readonly SignInManager<User> _signInManager;
    private readonly IUserService _userService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AccountController> _logger;

    private static readonly TimeSpan ResetCooldown = TimeSpan.FromSeconds(30);

    public AccountController(
        SignInManager<User> signInManager,
        IUserService userService,
        IMemoryCache cache,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userService = userService;
        _cache = cache;
        _logger = logger;
    }

    #region Login

    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(returnUrl ?? "/connect/authorize");
        }

        var model = new LoginViewModel
        {
            ReturnUrl = returnUrl ?? "/connect/authorize"
        };

        ViewData["Title"] = "Login";
        return View(model);
    }


    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        var returnUrl = model.ReturnUrl ?? "/connect/authorize";

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Login";
            return View(model);
        }

        // Find user by email
        var user = await _signInManager.UserManager.FindByEmailAsync(model.Email);
        if (user == null || !await _signInManager.UserManager.CheckPasswordAsync(user, model.Password))
        {
            _logger.LogWarning("Failed login attempt for {Email}", model.Email);
            ModelState.AddModelError(string.Empty, UiMessages.InvalidCredentials);
            ViewData["Title"] = "Login";
            return View(model);
        }

        // Check if account is locked
        if (await _signInManager.UserManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("Login attempt for locked account: {UserId}", user.Id);
            ModelState.AddModelError(string.Empty, UiMessages.LockedAccount);
            ViewData["Title"] = "Login";
            return View(model);
        }


        // Check if user can sign in
        if (!await _signInManager.CanSignInAsync(user))
        {
            _logger.LogWarning("User {UserId} cannot sign in", user.Id);
            ModelState.AddModelError(string.Empty, "Sign-in not allowed for this account.");
            ViewData["Title"] = "Login";
            return View(model);
        }

        // Create session ID
        var sessionId = ClaimsHelper.GenerateSessionId();

        // Create principal
        var principal = await _signInManager.CreateUserPrincipalAsync(user);

        var identity = (ClaimsIdentity)principal.Identity!;

        // Add session ID claim to the principal
        // OpenIddict will read this claim and include it in tokens

        identity.AddClaim(new Claim(
            OidcClaimTypes.SessionId,
            sessionId
        ));

        // Sign in to ASP.NET Core Identity
        // This creates the auth server's own cookie
        await HttpContext.SignInAsync(
            IdentityConstants.ApplicationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(30)
                    : DateTimeOffset.UtcNow.AddHours(8)
            });

        _logger.LogInformation(
                   "User logged in - UserId: {UserId}, SessionId: {SessionId}, RememberMe: {RememberMe}",
                   user.Id,
                   sessionId,
                   model.RememberMe);

        TempData["SuccessMessage"] = $"Welcome back, {user.FirstName ?? user.UserName}!";
        return LocalRedirect(returnUrl);
    }

    #endregion

    #region Register

    [HttpGet("register")]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(returnUrl ?? "/connect/authorize");
        }

        var model = new RegisterViewModel
        {
            ReturnUrl = returnUrl ?? "/connect/authorize"
        };

        ViewData["Title"] = "Register";
        return View(model);
    }


    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        var returnUrl = model.ReturnUrl ?? "/connect/authorize";

        if (!ModelState.IsValid)
        {
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

            _logger.LogInformation("✅ New user registered: {Email}", model.Email);
            TempData["SuccessMessage"] = UiMessages.AccountCreated;

            return RedirectToAction(nameof(Login), new { returnUrl });
        }
        catch (AvanciraException ex)
        {
            _logger.LogWarning(ex, "Registration failed for {Email}", model.Email);

            var errors = ex.ErrorMessages.Any() ? ex.ErrorMessages : new[] { ex.Message };
            foreach (var error in errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            ViewData["Title"] = "Register";
            return View(model);
        }
    }

    #endregion

    #region Forgot Password

    [HttpGet("forgot-password")]
    public IActionResult ForgotPassword()
    {
        ViewData["Title"] = "Forgot Password";
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost("forgot-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Forgot Password";
            return View(model);
        }

        // Rate limiting to prevent abuse
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var cacheKey = $"pwreset:{model.Email}:{ip}";
        var now = DateTimeOffset.UtcNow;

        if (_cache.TryGetValue(cacheKey, out DateTimeOffset lastAttempt))
        {
            var elapsed = now - lastAttempt;
            if (elapsed < ResetCooldown)
            {
                var waitSeconds = (int)(ResetCooldown - elapsed).TotalSeconds;
                ModelState.AddModelError(
                    string.Empty,
                    $"Please wait {waitSeconds} seconds before retrying.");

                ViewData["Title"] = "Forgot Password";
                return View(model);
            }
        }

        try
        {
            await _userService.ForgotPasswordAsync(
                new ForgotPasswordDto { Email = model.Email },
                HttpContext.RequestAborted);

            _cache.Set(cacheKey, now, ResetCooldown);
            _logger.LogInformation("Password reset requested for {Email}", model.Email);

            TempData["SuccessMessage"] = UiMessages.PasswordResetSent;
        }
        catch (Exception ex)
        {
            // Don't reveal if email exists (security best practice)
            _logger.LogWarning(ex, "Password reset attempt for {Email}", model.Email);
            TempData["SuccessMessage"] = UiMessages.PasswordResetSent;
        }

        return RedirectToAction(nameof(Login));
    }

    #endregion

    #region Logout

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var sessionId = User.FindFirstValue(OidcClaimTypes.SessionId);

        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

        _logger.LogInformation(
            "User logged out - UserId: {UserId}, SessionId: {SessionId}",
            userId,
            sessionId);

        TempData["SuccessMessage"] = "You have been logged out successfully.";
        return RedirectToAction(nameof(Login));
    }
    #endregion

    #region Access Denied

    [HttpGet("access-denied")]
    public IActionResult AccessDenied()
    {
        ViewData["Title"] = "Access Denied";
        return View();
    }
    #endregion
}

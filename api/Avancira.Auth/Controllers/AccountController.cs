using System.Security.Claims;
using Avancira.Application.Countries;
using Avancira.Application.Identity.Users.Abstractions;
using Avancira.Application.Identity.Users.Dtos;
using Avancira.Auth.Helpers;
using Avancira.Auth.Models.Account;
using Avancira.Domain.Common.Exceptions;
using Avancira.Infrastructure.Identity.Users;
using Avancira.Shared.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using OpenIddict.Abstractions;
using TimeZoneConverter;
using IdentityConstants = Microsoft.AspNetCore.Identity.IdentityConstants;

namespace Avancira.Auth.Controllers;

[AllowAnonymous]
[AutoValidateAntiforgeryToken]
[Route("account")]
[Produces("text/html")]
public partial class AccountController : Controller
{
    private readonly SignInManager<User> _signInManager;
    private readonly IUserService _userService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AccountController> _logger;
    private readonly ICountryService _countryService;

    private static readonly string CountryCacheKey = "cached_countries";
    private static readonly string TimeZoneCacheKey = "cached_timezones";

    // Cooldown configuration
    private const int PasswordResetCooldownSeconds = 60; // 1 minute
    private const string PasswordResetCooldownPrefix = "pwd_reset_cooldown_";

    public AccountController(
        SignInManager<User> signInManager,
        IUserService userService,
        IMemoryCache cache,
        ILogger<AccountController> logger,
        ICountryService countryService)
    {
        _signInManager = signInManager;
        _userService = userService;
        _cache = cache;
        _logger = logger;
        _countryService = countryService;
    }


    #region Login

    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return LocalRedirect(returnUrl ?? "/connect/authorize");

        ViewData["Title"] = "Login";
        return View(new LoginViewModel { ReturnUrl = returnUrl ?? "/connect/authorize" });
    }

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        ViewData["Title"] = "Login";

        if (!ModelState.IsValid)
            return View(model);

        var returnUrl = SanitizeReturnUrl(model.ReturnUrl);
        var userDto = await _userService.GetByEmailAsync(model.Email, HttpContext.RequestAborted);

        if (userDto == null)
        {
            ModelState.AddModelError(string.Empty, UiMessages.InvalidCredentials);
            return View(model);
        }

        // Validate password
        var user = new User { Id = userDto.Id, Email = userDto.Email, UserName = userDto.Email };
        var isValid = await _signInManager.UserManager.CheckPasswordAsync(user, model.Password);

        if (!isValid)
        {
            ModelState.AddModelError(string.Empty, UiMessages.InvalidCredentials);
            return View(model);
        }

        var sessionId = ClaimsHelper.GenerateSessionId();
        var principal = await _signInManager.CreateUserPrincipalAsync(user);
        ((ClaimsIdentity)principal.Identity!).AddClaim(new Claim(OidcClaimTypes.SessionId, sessionId));

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

        _logger.LogInformation("User logged in: {UserId}", user.Id);
        TempData["SuccessMessage"] = $"Welcome back, {userDto.FirstName ?? userDto.Email}!";

        return LocalRedirect(returnUrl);
    }

    #endregion


    #region Register

    [HttpGet("register")]
    public async Task<IActionResult> Register(string? returnUrl = null)
    {
        ViewData["Title"] = "Register";

        var model = new RegisterViewModel
        {
            ReturnUrl = returnUrl ?? "/connect/authorize",
            Countries = await GetCachedCountriesAsync(),
            TimeZones = GetCachedTimeZones()
        };

        return View(model);
    }

    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        ViewData["Title"] = "Register";

        if (!ModelState.IsValid)
        {
            await RebuildListsAsync(model);
            return View(model);
        }

        var dto = new RegisterUserDto
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            Password = model.Password,
            ConfirmPassword = model.ConfirmPassword,
            CountryCode = model.CountryCode,
            PhoneNumber = model.PhoneNumber,
            TimeZoneId = model.TimeZoneId,
            RegisterAsTutor = model.RegisterAsTutor,
            Gender = model.Gender,
            DateOfBirth = model.DateOfBirth,
            AcceptTerms = model.AcceptTerms
        };

        await _userService.RegisterAsync(dto, $"{Request.Scheme}://{Request.Host}", HttpContext.RequestAborted);

        TempData["SuccessMessage"] = "Your account has been created successfully.";
        return RedirectToAction(nameof(Login));
    }

    #endregion


    #region Logout

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

        _logger.LogInformation("User logged out - UserId: {UserId}", userId);
        TempData["SuccessMessage"] = "You have been logged out successfully.";

        return RedirectToAction(nameof(Login));
    }

    #endregion


    #region Forgot Password

    [HttpGet("forgot-password")]
    public IActionResult ForgotPassword(string? email = null)
    {
        ViewData["Title"] = "Forgot Password";

        var model = new ForgotPasswordViewModel
        {
            Email = email,
            RemainingCooldown = GetRemainingCooldown(email)
        };

        return View(model);
    }

    [HttpPost("forgot-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        ViewData["Title"] = "Forgot Password";

        if (!ModelState.IsValid)
        {
            model.RemainingCooldown = GetRemainingCooldown(model.Email);
            return View(model);
        }

        // Normalize email for consistent cooldown tracking
        var normalizedEmail = model.Email?.Trim().ToLowerInvariant();
        var cooldownKey = GetCooldownKey(normalizedEmail);
        var remainingCooldown = GetRemainingCooldown(normalizedEmail);

        // Check cooldown to prevent spam/abuse
        if (remainingCooldown > 0)
        {
            ModelState.AddModelError(string.Empty,
                $"Please wait {remainingCooldown} seconds before requesting another reset.");
            model.RemainingCooldown = remainingCooldown;
            return View(model);
        }

        try
        {
            // Attempt to send reset email
            await _userService.ForgotPasswordAsync(
                new ForgotPasswordDto { Email = model.Email },
                HttpContext.RequestAborted);

            // Set cooldown period after successful request
            _cache.Set(cooldownKey, DateTimeOffset.UtcNow,
                TimeSpan.FromSeconds(PasswordResetCooldownSeconds));

            _logger.LogInformation(
                "Password reset email sent successfully for {Email}",
                normalizedEmail);
        }
        catch (Exception ex)
        {
            // SECURITY: Do not reveal whether user exists
            // Still show success message and set cooldown to prevent enumeration
            _logger.LogWarning(ex,
                "Password reset request failed for {Email} - Reason: {Reason}",
                normalizedEmail, ex.Message);

            // Set cooldown even for failed attempts to prevent email enumeration attacks
            _cache.Set(cooldownKey, DateTimeOffset.UtcNow,
                TimeSpan.FromSeconds(PasswordResetCooldownSeconds));
        }

        // Store email in TempData for confirmation page
        TempData["ResetEmail"] = model.Email;

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }


    [HttpGet("forgot-password-confirmation")]
    public IActionResult ForgotPasswordConfirmation()
    {
        ViewData["Title"] = "Check Your Email";

        var email = TempData.Peek("ResetEmail") as string; // Peek to keep for resend
        var model = new ForgotPasswordConfirmationViewModel
        {
            Email = email,
            RemainingCooldown = GetRemainingCooldown(email)
        };

        return View(model);
    }

    #endregion


    #region Reset Password

    [HttpGet("reset-password")]
    public IActionResult ResetPassword(string? userId, string? token)
    {
        // Validate required parameters
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Reset password accessed with missing parameters");
            TempData["ErrorMessage"] = "Invalid or expired password reset link.";
            return RedirectToAction(nameof(Login));
        }

        var model = new ResetPasswordViewModel
        {
            UserId = userId,
            Token = token // Keep token as-is, don't encode
        };

        ViewData["Title"] = "Reset Password";
        return View(model);
    }

    [HttpPost("reset-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        ViewData["Title"] = "Reset Password";

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var dto = new ResetPasswordDto
            {
                UserId = model.UserId,
                Password = model.Password,
                ConfirmPassword = model.ConfirmPassword,
                Token = model.Token
            };

            await _userService.ResetPasswordAsync(dto, HttpContext.RequestAborted);

            _logger.LogInformation(
                "Password reset completed successfully for UserId: {UserId}",
                model.UserId);

            TempData["SuccessMessage"] = "Your password has been reset successfully. Please login with your new password.";

            return RedirectToAction(nameof(Login));
        }
        catch (AvanciraException ex)
        {
            // Handle known business exceptions
            var errors = ex.ErrorMessages.Any()
                ? ex.ErrorMessages
                : new[] { "Password reset failed. The link may be invalid or expired." };

            foreach (var msg in errors)
                ModelState.AddModelError(string.Empty, msg);

            _logger.LogWarning(ex,
                "Password reset failed for UserId: {UserId} - ValidationErrors: {Errors}",
                model.UserId, string.Join(", ", errors));

            return View(model);
        }
        catch (Exception ex)
        {
            // Handle unexpected exceptions
            _logger.LogError(ex,
                "Unexpected error resetting password for UserId: {UserId}",
                model.UserId);

            ModelState.AddModelError(string.Empty,
                "An unexpected error occurred. The reset link may be invalid or expired. Please request a new one.");

            return View(model);
        }
    }

    #endregion


    #region Helpers

    private static string SanitizeReturnUrl(string? returnUrl)
        => string.IsNullOrWhiteSpace(returnUrl) || !Uri.IsWellFormedUriString(returnUrl, UriKind.Relative)
            ? "/connect/authorize"
            : returnUrl;

    private async Task<IEnumerable<SelectListItem>> GetCachedCountriesAsync()
    {
        if (!_cache.TryGetValue(CountryCacheKey, out IEnumerable<SelectListItem>? countries))
        {
            var list = await _countryService.GetAllAsync();
            countries = list
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem(c.Name, c.Code))
                .ToList();

            _cache.Set(CountryCacheKey, countries, TimeSpan.FromHours(12));
        }
        return countries!;
    }

    private IEnumerable<SelectListItem> GetCachedTimeZones()
    {
        if (!_cache.TryGetValue(TimeZoneCacheKey, out IEnumerable<SelectListItem>? timeZones))
        {
            timeZones = TZConvert.KnownIanaTimeZoneNames
                .Select(id =>
                {
                    try
                    {
                        var tz = TimeZoneInfo.FindSystemTimeZoneById(id);
                        var offset = tz.BaseUtcOffset;
                        var sign = offset >= TimeSpan.Zero ? "+" : "-";
                        var hours = Math.Abs(offset.Hours).ToString("00");
                        var minutes = Math.Abs(offset.Minutes).ToString("00");
                        var parts = id.Split('/');
                        var display = parts.Length == 2
                            ? $"{parts[1].Replace('_', ' ')} ({parts[0].Replace('_', ' ')}) (UTC{sign}{hours}:{minutes})"
                            : $"{id.Replace('_', ' ')} (UTC{sign}{hours}:{minutes})";
                        return new SelectListItem(display, id);
                    }
                    catch
                    {
                        return null;
                    }
                })
                .Where(x => x != null)!
                .OrderBy(x => TimeZoneInfo.FindSystemTimeZoneById(x!.Value!).BaseUtcOffset)
                .ThenBy(x => x!.Text)
                .ToList()!;

            _cache.Set(TimeZoneCacheKey, timeZones, TimeSpan.FromHours(12));
        }
        return timeZones!;
    }

    private async Task RebuildListsAsync(RegisterViewModel model)
    {
        model.Countries = await GetCachedCountriesAsync();
        model.TimeZones = GetCachedTimeZones();
    }

    /// <summary>
    /// Generate cache key for cooldown tracking
    /// Uses email address to track per-user cooldowns
    /// </summary>
    private string GetCooldownKey(string? email)
    {
        var normalizedEmail = email?.Trim().ToLowerInvariant() ?? "unknown";
        return $"{PasswordResetCooldownPrefix}{normalizedEmail}";
    }

    /// <summary>
    /// Calculate remaining cooldown time in seconds
    /// Returns 0 if no cooldown is active
    /// </summary>
    private int GetRemainingCooldown(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return 0;

        var cooldownKey = GetCooldownKey(email);

        if (_cache.TryGetValue<DateTimeOffset>(cooldownKey, out var cooldownStart))
        {
            var elapsed = (DateTimeOffset.UtcNow - cooldownStart).TotalSeconds;
            var remaining = PasswordResetCooldownSeconds - (int)elapsed;
            return Math.Max(0, remaining);
        }

        return 0;
    }

    #endregion
}

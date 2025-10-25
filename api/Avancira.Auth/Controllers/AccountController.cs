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
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using PhoneNumbers;
using System.Security.Claims;
using TimeZoneConverter;
using static Pipelines.Sockets.Unofficial.SocketConnection;
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
    private readonly ICountryService _countryService;

    private static readonly TimeSpan ResetCooldown = TimeSpan.FromSeconds(30);

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
    public async Task<IActionResult> Register(string? returnUrl = null)
    {
        var model = new RegisterViewModel
        {
            ReturnUrl = returnUrl ?? "/connect/authorize",
            Countries = await GetCountriesAsync(),
            TimeZones = GetTimeZones()
        };

        return View(model);
    }

    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await RebuildListsAsync(model);
            return View(model);
        }

        try
        {
            var phoneUtil = PhoneNumberUtil.GetInstance();
            var parsed = phoneUtil.Parse(model.PhoneNumber, null);
            model.PhoneNumber = phoneUtil.Format(parsed, PhoneNumberFormat.E164);
        }
        catch (NumberParseException)
        {
            ModelState.AddModelError(nameof(model.PhoneNumber), "Invalid phone number format.");
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

    private async Task RebuildListsAsync(RegisterViewModel model)
    {
        model.Countries = await GetCountriesAsync();
        model.TimeZones = GetTimeZones();
    }

    private async Task<IEnumerable<SelectListItem>> GetCountriesAsync()
    {
        var countries = await _countryService.GetAllAsync();
        return countries
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem(c.Name, c.Code));
    }

    private static IEnumerable<SelectListItem> GetTimeZones()
    {
        // Load IANA time zones (portable across OS)
        var ianaZones = TZConvert.KnownIanaTimeZoneNames
            .Select(id =>
            {
                try
                {
                    var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(id);
                    var offset = tzInfo.BaseUtcOffset;
                    var sign = offset >= TimeSpan.Zero ? "+" : "-";
                    var hours = Math.Abs(offset.Hours).ToString("00");
                    var minutes = Math.Abs(offset.Minutes).ToString("00");

                    // Extract region and city (e.g., "Africa/Cairo" → Region="Africa", City="Cairo")
                    var parts = id.Split('/');
                    string display;

                    if (parts.Length == 2)
                    {
                        var region = parts[0].Replace('_', ' ');
                        var city = parts[1].Replace('_', ' ');
                        display = $"{city} ({region}) (UTC{sign}{hours}:{minutes})";
                    }
                    else
                    {
                        // fallback for single-part names
                        display = $"{id.Replace('_', ' ')} (UTC{sign}{hours}:{minutes})";
                    }

                    return new SelectListItem(display, id);
                }
                catch
                {
                    // skip invalid or unmapped zones (e.g. Antarctica/Troll on Windows)
                    return null;
                }
            })
            .Where(x => x != null)
            .OrderBy(x =>
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(x!.Value!);
                return tz.BaseUtcOffset;
            })
            .ThenBy(x => x!.Text)
            .ToList()!;

        return ianaZones!;
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

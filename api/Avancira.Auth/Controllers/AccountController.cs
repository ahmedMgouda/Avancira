using Avancira.Application.Countries;
using Avancira.Application.Identity.Users.Abstractions;
using Avancira.Application.Identity.Users.Dtos;
using Avancira.Auth.Helpers;
using Avancira.Auth.Models.Account;
using Avancira.Infrastructure.Identity.Users;
using Avancira.Shared.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using OpenIddict.Abstractions;
using System.Security.Claims;
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

    private static readonly TimeSpan ResetCooldown = TimeSpan.FromSeconds(30);
    private static readonly string CountryCacheKey = "cached_countries";
    private static readonly string TimeZoneCacheKey = "cached_timezones";

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

    // =============================================================
    //  LOGIN
    // =============================================================

    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return LocalRedirect(returnUrl ?? "/connect/authorize");

        return View(new LoginViewModel { ReturnUrl = returnUrl ?? "/connect/authorize" });
    }

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var returnUrl = SanitizeReturnUrl(model.ReturnUrl);
        var userDto = await _userService.GetByEmailAsync(model.Email, HttpContext.RequestAborted);

        if (userDto == null)
        {
            ModelState.AddModelError(string.Empty, UiMessages.InvalidCredentials);
            return View(model);
        }

        // Use ASP.NET Identity for password verification
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

    // =============================================================
    //  REGISTER
    // =============================================================

    [HttpGet("register")]
    public async Task<IActionResult> Register(string? returnUrl = null)
    {
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

    // =============================================================
    //  LOGOUT
    // =============================================================

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

    // =============================================================
    //  HELPERS
    // =============================================================

    private static string SanitizeReturnUrl(string? returnUrl)
        => string.IsNullOrWhiteSpace(returnUrl) || !Uri.IsWellFormedUriString(returnUrl, UriKind.Relative)
            ? "/connect/authorize"
            : returnUrl;

    private async Task<IEnumerable<SelectListItem>> GetCachedCountriesAsync()
    {
        if (!_cache.TryGetValue(CountryCacheKey, out IEnumerable<SelectListItem>? countries))
        {
            var list = await _countryService.GetAllAsync();
            countries = list.OrderBy(c => c.Name).Select(c => new SelectListItem(c.Name, c.Code)).ToList();
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
                    catch { return null; }
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
}

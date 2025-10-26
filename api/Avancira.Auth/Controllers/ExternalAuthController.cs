using Avancira.Application.Identity.Users.Abstractions;
using Avancira.Application.Identity.Users.Dtos;
using Avancira.Auth.Models.Account;
using Avancira.Infrastructure.Identity.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using System.Security.Claims;

namespace Avancira.Auth.Controllers;

[AllowAnonymous]
[Route("account")]
public partial class ExternalAuthController : Controller
{
    private static readonly HashSet<string> AllowedProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        GoogleDefaults.AuthenticationScheme,
        FacebookDefaults.AuthenticationScheme
    };

    private readonly SignInManager<User> _signInManager;
    private readonly IUserService _userService;
    private readonly ILogger<ExternalAuthController> _logger;

    public ExternalAuthController(SignInManager<User> signInManager, IUserService userService, ILogger<ExternalAuthController> logger)
    {
        _signInManager = signInManager;
        _userService = userService;
        _logger = logger;
    }

    // =============================================================
    //  INITIATE EXTERNAL LOGIN
    // =============================================================

    [HttpPost("external-login")]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin([FromForm] string provider, [FromForm] string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(provider) || !AllowedProviders.Contains(provider))
        {
            _logger.LogWarning("Invalid external provider: {Provider}", provider);
            return RedirectToAction("Login", "Account", new { error = "invalid_provider" });
        }

        var redirectUrl = Url.Action(nameof(ExternalCallback), new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);

        _logger.LogInformation("Initiating external login for {Provider}", provider);
        return Challenge(properties, provider);
    }

    // =============================================================
    //  CALLBACK FROM PROVIDER
    // =============================================================

    [HttpGet("external-callback")]
    public async Task<IActionResult> ExternalCallback([FromQuery] string? returnUrl = null)
    {
        returnUrl ??= "/connect/authorize";
        if (!Url.IsLocalUrl(returnUrl))
            returnUrl = "/";

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            _logger.LogWarning("External login info was null.");
            TempData["ErrorMessage"] = "External authentication failed. Please try again.";
            return RedirectToAction("Login", "Account");
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName);
        var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname);

        if (string.IsNullOrEmpty(email))
        {
            TempData["ErrorMessage"] = "Email permission is required.";
            return RedirectToAction("Login", "Account");
        }

        // Step 1: Already linked
        var linkedUserId = await _userService.GetLinkedUserIdAsync(info.LoginProvider, info.ProviderKey);
        if (linkedUserId is not null)
        {
            await SignInUserAsync(linkedUserId);
            return LocalRedirect(returnUrl);
        }

        // Step 2: Existing email but not linked
        var existingUser = await _userService.GetByEmailAsync(email, HttpContext.RequestAborted);
        if (existingUser is not null)
        {
            await _userService.LinkExternalLoginAsync(existingUser.Id, info.LoginProvider, info.ProviderKey);
            await SignInUserAsync(existingUser.Id);
            return LocalRedirect(returnUrl);
        }

        // Step 3: New user → Complete profile
        TempData["ExternalProvider"] = info.LoginProvider;
        TempData["ProviderKey"] = info.ProviderKey;
        TempData["Email"] = email;
        TempData["FirstName"] = firstName ?? "";
        TempData["LastName"] = lastName ?? "";
        TempData["ReturnUrl"] = returnUrl;

        return RedirectToAction("CompleteProfile", "ExternalAuth");
    }

    // =============================================================
    //  COMPLETE PROFILE
    // =============================================================

    [HttpGet("complete-profile")]
    public IActionResult CompleteProfile()
    {
        var model = new CompleteProfileViewModel
        {
            Provider = TempData["ExternalProvider"]?.ToString(),
            ProviderKey = TempData["ProviderKey"]?.ToString(),
            Email = TempData["Email"]?.ToString(),
            FirstName = TempData["FirstName"]?.ToString(),
            LastName = TempData["LastName"]?.ToString(),
            ReturnUrl = TempData["ReturnUrl"]?.ToString() ?? "/connect/authorize"
        };
        TempData.Keep();
        return View(model);
    }

    [HttpPost("complete-profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteProfile(CompleteProfileViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var dto = new SocialRegisterDto
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                CountryCode = model.CountryCode,
                TimeZoneId = model.TimeZoneId,
                Gender = model.Gender,
                RegisterAsTutor = false
            };

            var user = await _userService.RegisterExternalAsync(dto, HttpContext.RequestAborted);

            await _userService.LinkExternalLoginAsync(user.Id, model.Provider!, model.ProviderKey!);

            await SignInUserAsync(user.Id);

            return LocalRedirect(model.ReturnUrl ?? "/connect/authorize");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "External registration failed for {Email}", model.Email);
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
    }

    // =============================================================
    //  PRIVATE
    // =============================================================

    private async Task SignInUserAsync(string userId)
    {
        var user = await _userService.GetAsync(userId, HttpContext.RequestAborted);
        var principal = await _signInManager.CreateUserPrincipalAsync(new User
        {
            Id = user.Id,
            Email = user.Email,
            UserName = user.Email
        });
        await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, principal);
    }
}

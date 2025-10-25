using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Avancira.Infrastructure.Identity.Users;
using Avancira.Auth.Helpers;
using Avancira.Shared.Authorization;
using OpenIddict.Abstractions;

namespace Avancira.Auth.Controllers;

[AllowAnonymous]
[Route("account")]
public class ExternalAuthController : Controller
{
    private static readonly HashSet<string> AllowedProviders =
        new(StringComparer.OrdinalIgnoreCase)
        {
            GoogleDefaults.AuthenticationScheme,
            FacebookDefaults.AuthenticationScheme
        };

    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<ExternalAuthController> _logger;

    public ExternalAuthController(
        SignInManager<User> signInManager,
        UserManager<User> userManager,
        ILogger<ExternalAuthController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpPost("external-login")]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin([FromForm] string provider, [FromForm] string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(provider) || !AllowedProviders.Contains(provider))
        {
            _logger.LogWarning("Invalid external provider: {Provider}", provider);
            return RedirectToAction("Login", "Account", new { error = "invalid_provider", returnUrl });
        }

        // FIX 1: Ensure the callback URL matches exactly what's configured in AuthenticationExtensions
        var callbackUrl = Url.Action(
            nameof(ExternalCallback),
            "ExternalAuth",
            new { returnUrl },
            protocol: Request.Scheme,
            host: Request.Host.Value)!;

        _logger.LogInformation("External login initiated for {Provider}. Callback URL: {CallbackUrl}", provider, callbackUrl);

        var props = _signInManager.ConfigureExternalAuthenticationProperties(provider, callbackUrl);

        // FIX 2: Explicitly set the redirect URI to ensure consistency
        props.RedirectUri = callbackUrl;

        return Challenge(props, provider);
    }

    [HttpGet("external-callback")]
    public async Task<IActionResult> ExternalCallback([FromQuery] string? returnUrl = null, [FromQuery] string? remoteError = null)
    {
        _logger.LogInformation(
            "External callback received. Path: {Path}, Query: {Query}, HasError: {HasError}",
            HttpContext.Request.Path,
            HttpContext.Request.QueryString,
            !string.IsNullOrEmpty(remoteError));

        // FIX 3: Check for remote errors first
        if (!string.IsNullOrEmpty(remoteError))
        {
            _logger.LogWarning("External authentication error: {Error}", remoteError);
            TempData["ErrorMessage"] = $"Error from external provider: {remoteError}";
            return RedirectToAction("Login", "Account", new { returnUrl });
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            _logger.LogWarning("External login info missing - possible state mismatch or cookie issue");

            // Try to get more details about why it failed
            var authenticateResult = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
            if (!authenticateResult.Succeeded)
            {
                _logger.LogError(authenticateResult.Failure,
                    "External authentication failed: {ErrorMessage}",
                    authenticateResult.Failure?.Message ?? "Unknown");
            }

            TempData["ErrorMessage"] = "External authentication failed. Please try again.";
            return RedirectToAction("Login", "Account", new { returnUrl });
        }

        _logger.LogInformation(
            "External login callback received. Provider: {Provider}, ProviderKey: {ProviderKey}",
            info.LoginProvider,
            info.ProviderKey);

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("Email claim missing from external provider");
            TempData["ErrorMessage"] = "Email is required from the external provider.";
            return RedirectToAction("Login", "Account", new { returnUrl });
        }

        // Try existing login
        var signInResult = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider,
            info.ProviderKey,
            isPersistent: false,
            bypassTwoFactor: true);

        if (signInResult.Succeeded)
        {
            _logger.LogInformation("External login succeeded for existing user: {Email}", email);

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                // FIX 4: Add session ID claim for existing users
                await SignInUserWithSessionAsync(existingUser, false);

                if (IsProfileIncomplete(existingUser))
                {
                    return RedirectToAction("CompleteProfile", "Account", new { returnUrl });
                }
            }

            return LocalRedirect(returnUrl ?? "/connect/authorize");
        }

        // Create user if missing
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new User
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "",
                LastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? ""
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                _logger.LogError("User creation failed: {Errors}",
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
                TempData["ErrorMessage"] = "Failed to create user account.";
                return RedirectToAction("Login", "Account", new { returnUrl });
            }

            _logger.LogInformation("New user created via external login: {Email}", email);
            await _userManager.AddLoginAsync(user, info);
        }
        else
        {
            // User exists but doesn't have this external login
            await _userManager.AddLoginAsync(user, info);
            _logger.LogInformation("External login added to existing user: {Email}", email);
        }

        // FIX 5: Sign in with session ID
        await SignInUserWithSessionAsync(user, false);

        if (IsProfileIncomplete(user))
        {
            return RedirectToAction("CompleteProfile", "Account", new { returnUrl });
        }

        TempData["SuccessMessage"] = $"Welcome, {user.FirstName ?? user.UserName}!";
        return LocalRedirect(returnUrl ?? "/connect/authorize");
    }

    // FIX 6: New method to sign in with session ID (matching AccountController pattern)
    private async Task SignInUserWithSessionAsync(User user, bool isPersistent)
    {
        // Generate session ID
        var sessionId = ClaimsHelper.GenerateSessionId();

        // Create principal with all claims
        var principal = await _signInManager.CreateUserPrincipalAsync(user);
        var identity = (ClaimsIdentity)principal.Identity!;

        // Add OpenIddict required claims
        AddOpenIddictClaims(identity, user);

        // Add session ID claim
        identity.AddClaim(new Claim(OidcClaimTypes.SessionId, sessionId));

        // Sign in with proper authentication properties
        await HttpContext.SignInAsync(
            IdentityConstants.ApplicationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = isPersistent
                    ? DateTimeOffset.UtcNow.AddDays(30)
                    : DateTimeOffset.UtcNow.AddHours(8)
            });

        _logger.LogInformation(
            "External user signed in - UserId: {UserId}, SessionId: {SessionId}",
            user.Id,
            sessionId);
    }

    private static void AddOpenIddictClaims(ClaimsIdentity identity, User user)
    {
        AddClaimIfMissing(identity, OpenIddictConstants.Claims.Subject, user.Id);
        AddClaimIfMissing(identity, OpenIddictConstants.Claims.Name, user.UserName);
        AddClaimIfMissing(identity, OpenIddictConstants.Claims.GivenName, user.FirstName);
        AddClaimIfMissing(identity, OpenIddictConstants.Claims.FamilyName, user.LastName);
        AddClaimIfMissing(identity, OpenIddictConstants.Claims.Email, user.Email);
        AddClaimIfMissing(
            identity,
            OpenIddictConstants.Claims.EmailVerified,
            user.EmailConfirmed.ToString().ToLowerInvariant(),
            ClaimValueTypes.Boolean);
    }

    private static void AddClaimIfMissing(ClaimsIdentity identity, string type, string? value, string? valueType = null)
    {
        if (string.IsNullOrEmpty(value))
            return;
        if (!identity.HasClaim(c => c.Type == type))
        {
            var claim = valueType is null
                ? new Claim(type, value)
                : new Claim(type, value, valueType);
            identity.AddClaim(claim);
        }
    }

    private static bool IsProfileIncomplete(User user)
    {
        return string.IsNullOrWhiteSpace(user.FirstName)
            || string.IsNullOrWhiteSpace(user.LastName)
            || string.IsNullOrWhiteSpace(user.CountryCode)
            || string.IsNullOrWhiteSpace(user.PhoneNumber)
            || string.IsNullOrWhiteSpace(user.TimeZoneId)
            || user.DateOfBirth is null
            || string.IsNullOrWhiteSpace(user.Gender);
    }
}
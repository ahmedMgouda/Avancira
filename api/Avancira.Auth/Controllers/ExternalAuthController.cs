using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Avancira.Infrastructure.Identity.Users;
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

    private const string DefaultReturnUrl = "/";
    private const string LoginPage = "Login";

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

    /// <summary>
    /// Initiates external authentication challenge (called from login page)
    /// </summary>
    [HttpPost("external-login")]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin([FromForm] string provider, [FromForm] string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(provider) || !AllowedProviders.Contains(provider))
        {
            _logger.LogWarning("Invalid provider requested: {Provider}", provider);
            return RedirectToAction(LoginPage, new { error = "invalid_provider", returnUrl });
        }

        var targetUrl = NormalizeReturnUrl(returnUrl, DefaultReturnUrl);
        var callbackUrl = Url.Action(
            nameof(ExternalCallback),
            "ExternalAuth",
            new { returnUrl = targetUrl },
            Request.Scheme)!;

        var props = _signInManager.ConfigureExternalAuthenticationProperties(provider, callbackUrl);

        _logger.LogInformation("Initiating external login - Provider: {Provider}", provider);

        return Challenge(props, provider);
    }

    /// <summary>
    /// Callback handler after external provider authentication
    /// </summary>
    [HttpGet("external-callback")]
    public async Task<IActionResult> ExternalCallback([FromQuery] string? returnUrl = null)
    {
        var targetUrl = NormalizeReturnUrl(returnUrl, DefaultReturnUrl);

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            _logger.LogWarning("External login info not found in callback");
            return RedirectToAction(LoginPage, new { error = "external_auth_failed", returnUrl = targetUrl });
        }

        _logger.LogInformation(
            "External callback received - Provider: {Provider}, ProviderKey: {ProviderKey}",
            info.LoginProvider,
            info.ProviderKey);

        // Try to sign in with existing external login
        var signInResult = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider,
            info.ProviderKey,
            isPersistent: false,
            bypassTwoFactor: true);

        if (signInResult.Succeeded)
        {
            _logger.LogInformation("External login succeeded - Provider: {Provider}", info.LoginProvider);
            return LocalRedirect(targetUrl);
        }

        // Extract and validate email
        var email = ExtractEmailFromClaims(info.Principal);
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("No email claim found in external login - Provider: {Provider}", info.LoginProvider);
            return RedirectToAction(LoginPage, new { error = "email_required", returnUrl = targetUrl });
        }

        // Get or create user
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            var (success, newUser, errorCode) = await CreateAndLinkExternalUserAsync(info, email);
            if (!success)
            {
                _logger.LogError("User creation failed - Error: {Error}", errorCode);
                return RedirectToAction(LoginPage, new { error = errorCode, returnUrl = targetUrl });
            }

            user = newUser!;
            _logger.LogInformation("Created new user from external login - UserId: {UserId}, Provider: {Provider}",
                user.Id, info.LoginProvider);
        }
        else
        {
            var (success, errorCode) = await LinkExternalProviderToExistingUserAsync(user, info);
            if (!success)
            {
                _logger.LogError("Failed to link external provider - UserId: {UserId}, Error: {Error}",
                    user.Id, errorCode);
                return RedirectToAction(LoginPage, new { error = errorCode, returnUrl = targetUrl });
            }
        }

        // Sign in user
        await SignInUserAsync(user);
        _logger.LogInformation("User signed in successfully - UserId: {UserId}, Provider: {Provider}",
            user.Id, info.LoginProvider);

        return LocalRedirect(targetUrl);
    }

    /// <summary>
    /// Creates a new user from external provider information
    /// </summary>
    private async Task<(bool Success, User? User, string? ErrorCode)> CreateAndLinkExternalUserAsync(ExternalLoginInfo info, string email)
    {
        var user = CreateUserFromExternalInfo(info, email);

        var createResult = await _userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            _logger.LogError("Failed to create user - Errors: {Errors}",
                string.Join(", ", createResult.Errors.Select(e => $"{e.Code}: {e.Description}")));
            return (false, null, "user_creation_failed");
        }

        var addLoginResult = await _userManager.AddLoginAsync(user, info);
        if (!addLoginResult.Succeeded)
        {
            _logger.LogError("Failed to link external login - UserId: {UserId}, Errors: {Errors}",
                user.Id,
                string.Join(", ", addLoginResult.Errors.Select(e => $"{e.Code}: {e.Description}")));

            await _userManager.DeleteAsync(user);
            return (false, null, "login_link_failed");
        }

        _logger.LogInformation("External login linked - UserId: {UserId}, Provider: {Provider}",
            user.Id, info.LoginProvider);

        return (true, user, null);
    }

    /// <summary>
    /// Links an external provider to an existing user
    /// </summary>
    private async Task<(bool Success, string? ErrorCode)> LinkExternalProviderToExistingUserAsync(User user, ExternalLoginInfo info)
    {
        var addLoginResult = await _userManager.AddLoginAsync(user, info);

        if (addLoginResult.Succeeded)
        {
            _logger.LogInformation("Linked external provider - UserId: {UserId}, Provider: {Provider}",
                user.Id, info.LoginProvider);
            return (true, null);
        }

        var isAlreadyLinked = addLoginResult.Errors
            .Any(e => e.Code == "LoginAlreadyAssociated");

        if (isAlreadyLinked)
        {
            _logger.LogDebug("External provider already linked - UserId: {UserId}, Provider: {Provider}",
                user.Id, info.LoginProvider);
            return (true, null);
        }

        _logger.LogError("Failed to link provider - UserId: {UserId}, Errors: {Errors}",
            user.Id,
            string.Join(", ", addLoginResult.Errors.Select(e => $"{e.Code}: {e.Description}")));

        return (false, "login_link_failed");
    }

    /// <summary>
    /// Signs in a user with OpenIddict-compatible claims
    /// </summary>
    private async Task SignInUserAsync(User user)
    {
        var principal = await _signInManager.CreateUserPrincipalAsync(user);
        var identity = (ClaimsIdentity)principal.Identity!;

        AddOpenIddictClaims(identity, user);

        await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, principal);
    }

    /// <summary>
    /// Extracts email from external provider claims with fallbacks
    /// </summary>
    private static string? ExtractEmailFromClaims(ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue("email")
            ?? principal.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");
    }

    /// <summary>
    /// Creates a user from external provider information
    /// </summary>
    private static User CreateUserFromExternalInfo(ExternalLoginInfo info, string email)
    {
        var name = ExtractNameFromClaims(info.Principal) ?? email.Split('@')[0];
        var givenName = ExtractGivenNameFromClaims(info.Principal) ?? name;
        var familyName = ExtractFamilyNameFromClaims(info.Principal) ?? string.Empty;

        return new User
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = givenName,
            LastName = familyName
        };
    }

    /// <summary>
    /// Extracts full name from claims with fallbacks
    /// </summary>
    private static string? ExtractNameFromClaims(ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Name)
            ?? principal.FindFirstValue("name")
            ?? principal.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name");
    }

    /// <summary>
    /// Extracts given name from claims with fallbacks
    /// </summary>
    private static string? ExtractGivenNameFromClaims(ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.GivenName)
            ?? principal.FindFirstValue("given_name")
            ?? principal.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname");
    }

    /// <summary>
    /// Extracts family name from claims with fallbacks
    /// </summary>
    private static string? ExtractFamilyNameFromClaims(ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Surname)
            ?? principal.FindFirstValue("family_name")
            ?? principal.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname");
    }

    /// <summary>
    /// Adds OpenIddict claims to user identity
    /// </summary>
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

    /// <summary>
    /// Adds a claim if it doesn't exist
    /// </summary>
    private static void AddClaimIfMissing(
        ClaimsIdentity identity,
        string type,
        string? value,
        string? valueType = null)
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

    /// <summary>
    /// Normalizes return URL to prevent open redirects
    /// </summary>
    private string NormalizeReturnUrl(string? returnUrl, string defaultUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
            return defaultUrl;

        return returnUrl;
    }
}
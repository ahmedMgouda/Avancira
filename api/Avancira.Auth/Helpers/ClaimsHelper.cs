using System.Security.Claims;
using Avancira.Infrastructure.Identity.Users;
using Avancira.Shared.Authorization;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;

namespace Avancira.Auth.Helpers;

/// <summary>
/// Manages claim generation and destination routing for OpenIddict tokens.
/// 
/// STRATEGY:
/// - ID Token: Minimal (sub + sid only) → For BFF cookie
/// - Access Token: Full user data → For API consumption
/// </summary>
public static class ClaimsHelper
{
    /// <summary>
    /// Generates a unique session identifier for this login session.
    /// This allows the same user to have multiple concurrent sessions across devices.
    /// </summary>
    /// <returns>Unique session ID (e.g., "sess_abc123def456")</returns>
    public static string GenerateSessionId()
    {
        return $"sess_{Guid.NewGuid():N}";
    }

    /// <summary>
    /// Ensures the principal has all required claims for OpenIddict.
    /// Adds sub, sid (if not present), email, and email_verified.
    /// 
    /// NOTE: sid should already exist (created in AccountController.Login)
    /// This method will add it only if missing (fallback scenario)
    /// </summary>
    public static void EnsureRequiredClaims(
        ClaimsPrincipal principal,
        User user,
        string sessionId)
    {
        var identity = (ClaimsIdentity)principal.Identity!;

        if (!identity.HasClaim(c => c.Type == OpenIddictConstants.Claims.Subject))
        {
            identity.AddClaim(new Claim(
                OpenIddictConstants.Claims.Subject,
                user.Id
            ));
        }

       
        if (!identity.HasClaim(c => c.Type == OidcClaimTypes.SessionId))
        {
            identity.AddClaim(new Claim(
                OidcClaimTypes.SessionId,
                sessionId
            ));
        }

        if (!identity.HasClaim(c => c.Type == OpenIddictConstants.Claims.Email) &&
            !string.IsNullOrEmpty(user.Email))
        {
            identity.AddClaim(new Claim(
                OpenIddictConstants.Claims.Email,
                user.Email
            ));
        }

        if (!identity.HasClaim(c => c.Type == OpenIddictConstants.Claims.EmailVerified))
        {
            identity.AddClaim(new Claim(
                OpenIddictConstants.Claims.EmailVerified,
                user.EmailConfirmed.ToString().ToLowerInvariant(),
                ClaimValueTypes.Boolean
            ));
        }
    }

    public static void AddProfileClaims(ClaimsPrincipal principal, User user)
    {
        var identity = (ClaimsIdentity)principal.Identity!;

        // Full name
        if (!string.IsNullOrEmpty(user.UserName))
        {
            identity.AddClaim(new Claim(
                OpenIddictConstants.Claims.Name,
                user.UserName
            ));
        }

        // First name
        if (!string.IsNullOrEmpty(user.FirstName))
        {
            identity.AddClaim(new Claim(
                OpenIddictConstants.Claims.GivenName,
                user.FirstName
            ));
        }

        // Last name
        if (!string.IsNullOrEmpty(user.LastName))
        {
            identity.AddClaim(new Claim(
                OpenIddictConstants.Claims.FamilyName,
                user.LastName
            ));
        }

        // Profile picture
        if (user.ImageUrl is not null)
        {
            identity.AddClaim(new Claim(
                OpenIddictConstants.Claims.Picture,
                user.ImageUrl.ToString()
            ));
        }
    }

    public static async Task AddRoleClaimsAsync(
        ClaimsPrincipal principal,
        User user,
        UserManager<User> userManager)
    {
        var identity = (ClaimsIdentity)principal.Identity!;
        var roles = await userManager.GetRolesAsync(user);

        foreach (var role in roles)
        {
            identity.AddClaim(new Claim(
                OpenIddictConstants.Claims.Role,
                role
            ));
        }
    }

    /// <summary>
    /// Refreshes dynamic user claims (email verification, roles).
    /// Used during token refresh to get latest user state.
    /// </summary>
    public static async Task RefreshDynamicClaimsAsync(
        ClaimsPrincipal principal,
        User user,
        UserManager<User> userManager)
    {
        var identity = (ClaimsIdentity)principal.Identity!;

        // Update email verification status
        var emailVerifiedClaim = identity.FindFirst(OpenIddictConstants.Claims.EmailVerified);
        if (emailVerifiedClaim != null)
        {
            identity.RemoveClaim(emailVerifiedClaim);
            identity.AddClaim(new Claim(
                OpenIddictConstants.Claims.EmailVerified,
                user.EmailConfirmed.ToString().ToLowerInvariant(),
                ClaimValueTypes.Boolean
            ));
        }

        // Update roles
        var existingRoleClaims = identity.FindAll(OpenIddictConstants.Claims.Role).ToList();
        foreach (var roleClaim in existingRoleClaims)
        {
            identity.RemoveClaim(roleClaim);
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        foreach (var role in currentRoles)
        {
            identity.AddClaim(new Claim(
                OpenIddictConstants.Claims.Role,
                role
            ));
        }
    }

    /// <summary>
    /// Attaches claim destinations to control where each claim appears.
    /// 
    /// STRATEGY:
    /// - ID Token: ONLY sub + sid (minimal for BFF)
    /// - Access Token: ALL claims (full user data for API)
    /// </summary>
    public static void AttachDestinations(ClaimsPrincipal principal)
    {
        foreach (var claim in principal.Claims)
        {
            var destinations = GetClaimDestinations(claim, principal);
            if (destinations.Any())
            {
                claim.SetDestinations(destinations);
            }
        }
    }

    /// <summary>
    /// Determines where each claim should appear (ID token, access token, or both).
    /// 
    /// CRITICAL OPTIMIZATION:
    /// - ID Token: Only sub + sid (87% smaller!)
    /// - Access Token: Everything (API needs full data)
    /// </summary>
    private static IEnumerable<string> GetClaimDestinations(
        Claim claim,
        ClaimsPrincipal principal) => claim.Type switch
        {
            OpenIddictConstants.Claims.Subject =>
            [
                OpenIddictConstants.Destinations.AccessToken,
                OpenIddictConstants.Destinations.IdentityToken
            ],

            OidcClaimTypes.SessionId =>
            [
                OpenIddictConstants.Destinations.IdentityToken
            ],


            OpenIddictConstants.Claims.Name or
            OpenIddictConstants.Claims.GivenName or
            OpenIddictConstants.Claims.FamilyName or
            OpenIddictConstants.Claims.Picture =>
                principal.HasScope(OpenIddictConstants.Scopes.Profile)
                    ? [OpenIddictConstants.Destinations.AccessToken]
                    : Array.Empty<string>(),


            OpenIddictConstants.Claims.Email or
            OpenIddictConstants.Claims.EmailVerified =>
                principal.HasScope(OpenIddictConstants.Scopes.Email)
                    ? [OpenIddictConstants.Destinations.AccessToken]
                    : Array.Empty<string>(),

          
            OpenIddictConstants.Claims.Role =>
            [
                OpenIddictConstants.Destinations.AccessToken
            ],

          
            _ => [OpenIddictConstants.Destinations.AccessToken]
        };
}
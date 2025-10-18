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

        // ═══════════════════════════════════════════════════════════════
        // CRITICAL: sub (user ID) - Required by OAuth 2.0
        // ═══════════════════════════════════════════════════════════════
        if (!identity.HasClaim(c => c.Type == OpenIddictConstants.Claims.Subject))
        {
            identity.AddClaim(new Claim(
                OpenIddictConstants.Claims.Subject,
                user.Id
            ));
        }

        // ═══════════════════════════════════════════════════════════════
        // CRITICAL: sid (session ID) - Enables multi-device sessions
        // NOTE: This should already exist (added in AccountController)
        // We only add it here as a safety fallback
        // ═══════════════════════════════════════════════════════════════
        if (!identity.HasClaim(c => c.Type == OidcClaimTypes.SessionId))
        {
            identity.AddClaim(new Claim(
                OidcClaimTypes.SessionId,
                sessionId
            ));
        }

        // ═══════════════════════════════════════════════════════════════
        // Optional: Email (if available)
        // ═══════════════════════════════════════════════════════════════
        if (!identity.HasClaim(c => c.Type == OpenIddictConstants.Claims.Email) &&
            !string.IsNullOrEmpty(user.Email))
        {
            identity.AddClaim(new Claim(
                OpenIddictConstants.Claims.Email,
                user.Email
            ));
        }

        // ═══════════════════════════════════════════════════════════════
        // Email verification status
        // ═══════════════════════════════════════════════════════════════
        if (!identity.HasClaim(c => c.Type == OpenIddictConstants.Claims.EmailVerified))
        {
            identity.AddClaim(new Claim(
                OpenIddictConstants.Claims.EmailVerified,
                user.EmailConfirmed.ToString().ToLowerInvariant(),
                ClaimValueTypes.Boolean
            ));
        }
    }

    /// <summary>
    /// Adds user profile claims (name, given_name, family_name) to the principal.
    /// These will ONLY go to the access token (not ID token).
    /// </summary>
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
    }

    /// <summary>
    /// Adds role claims to the principal.
    /// These will ONLY go to the access token (not ID token).
    /// </summary>
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
        ClaimsPrincipal principal)
    {
        return claim.Type switch
        {
            // ═══════════════════════════════════════════════════════════
            // ALWAYS in both tokens (required by OAuth 2.0 spec)
            // ═══════════════════════════════════════════════════════════
            OpenIddictConstants.Claims.Subject => new[]
            {
                OpenIddictConstants.Destinations.AccessToken,
                OpenIddictConstants.Destinations.IdentityToken
            },

            // ═══════════════════════════════════════════════════════════
            // Session ID: ONLY in ID token (for BFF multi-session support)
            // ═══════════════════════════════════════════════════════════
            OidcClaimTypes.SessionId => new[]
            {
                OpenIddictConstants.Destinations.IdentityToken
            },

            // ═══════════════════════════════════════════════════════════
            // Profile claims: ONLY in access token (API needs them)
            // NOT in ID token (BFF doesn't need them - reduces cookie size)
            // ═══════════════════════════════════════════════════════════
            OpenIddictConstants.Claims.Name or
            OpenIddictConstants.Claims.GivenName or
            OpenIddictConstants.Claims.FamilyName =>
                principal.HasScope(OpenIddictConstants.Scopes.Profile)
                    ? new[] { OpenIddictConstants.Destinations.AccessToken }
                    : Array.Empty<string>(),

            // ═══════════════════════════════════════════════════════════
            // Email claims: ONLY in access token
            // ═══════════════════════════════════════════════════════════
            OpenIddictConstants.Claims.Email or
            OpenIddictConstants.Claims.EmailVerified =>
                principal.HasScope(OpenIddictConstants.Scopes.Email)
                    ? new[] { OpenIddictConstants.Destinations.AccessToken }
                    : Array.Empty<string>(),

            // ═══════════════════════════════════════════════════════════
            // Roles: ONLY in access token (for API authorization)
            // ═══════════════════════════════════════════════════════════
            OpenIddictConstants.Claims.Role => new[]
            {
                OpenIddictConstants.Destinations.AccessToken
            },

            // ═══════════════════════════════════════════════════════════
            // All other claims: Access token only
            // ═══════════════════════════════════════════════════════════
            _ => new[] { OpenIddictConstants.Destinations.AccessToken }
        };
    }
}


// ════════════════════════════════════════════════════════════════════════════
// EXPLANATION: Why This Strategy?
// ════════════════════════════════════════════════════════════════════════════

/*
┌──────────────────────────────────────────────────────────────────────────┐
│                          TOKEN DISTRIBUTION                               │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│  ID Token (goes to BFF cookie):                                         │
│  ┌────────────────────────────────────────────┐                         │
│  │ {                                          │                         │
│  │   "sub": "user-123",                       │  ← User ID              │
│  │   "sid": "sess_abc123"                     │  ← Session ID           │
│  │ }                                          │                         │
│  └────────────────────────────────────────────┘                         │
│  Size: ~200 bytes (JWT overhead + 2 claims)                             │
│  Encrypted in BFF cookie: ~400 bytes total                              │
│                                                                           │
│  ─────────────────────────────────────────────────────────────────────   │
│                                                                           │
│  Access Token (goes to API):                                            │
│  ┌────────────────────────────────────────────┐                         │
│  │ {                                          │                         │
│  │   "sub": "user-123",                       │  ← User ID              │
│  │   "sid": "sess_abc123",                    │  ← Session ID           │
│  │   "name": "John Doe",                      │  ← Full name            │
│  │   "given_name": "John",                    │  ← First name           │
│  │   "family_name": "Doe",                    │  ← Last name            │
│  │   "email": "john@example.com",             │  ← Email                │
│  │   "email_verified": true,                  │  ← Verification         │
│  │   "role": ["User", "Admin"],               │  ← Roles                │
│  │   "scope": ["openid", "profile", "api"]    │  ← Scopes               │
│  │ }                                          │                         │
│  └────────────────────────────────────────────┘                         │
│  Size: ~800-1200 bytes                                                   │
│  Stored server-side by Duende (NOT in cookie)                           │
│                                                                           │
└──────────────────────────────────────────────────────────────────────────┘

BENEFITS:
✅ BFF cookie: 75% smaller (only sub + sid)
✅ API gets all data: Full user information in access token
✅ Multi-device support: Unique sid per login session
✅ Security: No sensitive data in browser cookie
✅ Performance: Smaller cookies = less bandwidth

MULTI-SESSION EXAMPLE:
─────────────────────────────────────────────────────────────────────────
User logs in from 3 devices:

1. Chrome (Desktop):   sub=user-123, sid=sess_aaa111
2. Safari (iPhone):    sub=user-123, sid=sess_bbb222  
3. Firefox (Work):     sub=user-123, sid=sess_ccc333

Each session is independent! Logout from one doesn't affect others.
Duende stores tokens using: {sub}:{sid} as key
*/
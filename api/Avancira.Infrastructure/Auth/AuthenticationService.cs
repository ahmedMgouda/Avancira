using Avancira.Application.Common;
using Avancira.Application.Identity;
using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Tokens.Dtos;
using FluentValidation;
using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Validation;

namespace Avancira.Infrastructure.Auth;

public class AuthenticationService : IAuthenticationService
{
    private readonly IClientInfoService _clientInfoService;
    private readonly ITokenEndpointClient _tokenClient;
    private readonly ISessionService _sessionService;
    private readonly IValidator<TokenRequestParams> _validator;
    private readonly string _scope;
    private readonly IRefreshTokenCookieService _refreshTokenCookieService;
    private readonly IOpenIddictValidationService _validationService;

    public AuthenticationService(
        IClientInfoService clientInfoService,
        ITokenEndpointClient tokenClient,
        ISessionService sessionService,
        IValidator<TokenRequestParams> validator,
        IOptions<AuthScopeOptions> scopeOptions,
        IRefreshTokenCookieService refreshTokenCookieService,
        IOpenIddictValidationService validationService)
    {
        _clientInfoService = clientInfoService;
        _tokenClient = tokenClient;
        _sessionService = sessionService;
        _validator = validator;
        _scope = scopeOptions.Value.Scope;
        _refreshTokenCookieService = refreshTokenCookieService;
        _validationService = validationService;
    }

    public async Task<TokenPair> ExchangeCodeAsync(string code, string codeVerifier, string redirectUri)
    {
        var request = new TokenRequestParams
        {
            GrantType = AuthConstants.GrantTypes.AuthorizationCode,
            Code = code,
            RedirectUri = redirectUri,
            CodeVerifier = codeVerifier
        };

        await _validator.ValidateAndThrowAsync(request);

        return await RequestTokenAsync(request, async (pair, clientInfo) =>
        {
            var principal = await ValidateTokenAsync(pair.Token);
            var userId = GetUserId(principal);
            var sessionId = GetSessionId(principal);
            await _sessionService.StoreSessionAsync(userId, sessionId, clientInfo, pair.RefreshTokenExpiryTime);
        });
    }

    public async Task<TokenPair> GenerateTokenAsync(string userId)
    {
        var request = new TokenRequestParams
        {
            GrantType = AuthConstants.GrantTypes.UserId,
            UserId = userId,
            Scope = _scope
        };

        await _validator.ValidateAndThrowAsync(request);

        return await RequestTokenAsync(request, async (pair, clientInfo) =>
        {
            var principal = await ValidateTokenAsync(pair.Token);
            var tokenUserId = GetUserId(principal);
            var sessionId = GetSessionId(principal);
            await _sessionService.StoreSessionAsync(tokenUserId, sessionId, clientInfo, pair.RefreshTokenExpiryTime);
        });
    }

    public async Task<TokenPair> RefreshTokenAsync(string refreshToken)
    {
        var request = new TokenRequestParams
        {
            GrantType = AuthConstants.GrantTypes.RefreshToken,
            RefreshToken = refreshToken
        };

        await _validator.ValidateAndThrowAsync(request);

        return await RequestTokenAsync(request, async (pair, _) =>
        {
            var principal = await ValidateTokenAsync(pair.Token);
            var userId = GetUserId(principal);
            var sessionId = GetSessionId(principal);
            await _sessionService.UpdateSessionAsync(userId, sessionId, pair.RefreshTokenExpiryTime);
        });
    }

    private async Task<TokenPair> RequestTokenAsync(
        TokenRequestParams parameters,
        Func<TokenPair, ClientInfo, Task>? postProcess = null)
    {
        var clientInfo = await _clientInfoService.GetClientInfoAsync();
        var updated = parameters with { DeviceId = clientInfo.DeviceId };

        var pair = await _tokenClient.RequestTokenAsync(updated);

        try
        {
            if (postProcess != null)
            {
                await postProcess(pair, clientInfo);
            }

            _refreshTokenCookieService.SetRefreshTokenCookie(pair.RefreshToken, pair.RefreshTokenExpiryTime);
        }
        catch
        {
            _refreshTokenCookieService.SetRefreshTokenCookie(string.Empty, DateTime.UtcNow.AddDays(-1));
            throw;
        }

        return pair;
    }

    private async Task<ClaimsPrincipal> ValidateTokenAsync(string token)
    {
        var principal = await _validationService.ValidateAccessTokenAsync(token);
        if (principal is null)
        {
            throw new SecurityTokenException("Token validation failed");
        }

        return principal;
    }

    private static string GetUserId(ClaimsPrincipal principal) =>
        principal.FindFirst(OpenIddictConstants.Claims.Subject)?.Value ?? string.Empty;

    private static Guid GetSessionId(ClaimsPrincipal principal)
    {
        var sid = principal.Claims.FirstOrDefault(c => c.Type == AuthConstants.Claims.SessionId)?.Value;
        return Guid.TryParse(sid, out var id) ? id : Guid.Empty;
    }
}

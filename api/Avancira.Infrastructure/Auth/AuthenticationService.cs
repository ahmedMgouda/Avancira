using Avancira.Application.Common;
using Avancira.Application.Identity;
using System.IdentityModel.Tokens.Jwt;
using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Tokens.Dtos;
using FluentValidation;
using System;
using System.Linq;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Avancira.Application.Auth.Jwt;

namespace Avancira.Infrastructure.Auth;

public class AuthenticationService : IAuthenticationService
{
    private readonly IClientInfoService _clientInfoService;
    private readonly ITokenEndpointClient _tokenClient;
    private readonly ISessionService _sessionService;
    private readonly IValidator<TokenRequestParams> _validator;
    private readonly TokenHashingOptions _options;
    private readonly JwtOptions _jwtOptions;
    private readonly string _scope;
    private readonly IRefreshTokenCookieService _refreshTokenCookieService;

    public AuthenticationService(
        IClientInfoService clientInfoService,
        ITokenEndpointClient tokenClient,
        ISessionService sessionService,
        IValidator<TokenRequestParams> validator,
        IOptions<TokenHashingOptions> options,
        IOptions<JwtOptions> jwtOptions,
        IOptions<AuthScopeOptions> scopeOptions,
        IRefreshTokenCookieService refreshTokenCookieService)
    {
        _clientInfoService = clientInfoService;
        _tokenClient = tokenClient;
        _sessionService = sessionService;
        _validator = validator;
        _options = options.Value;
        _jwtOptions = jwtOptions.Value;
        _scope = scopeOptions.Value.Scope;
        _refreshTokenCookieService = refreshTokenCookieService;
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
            var jwt = ParseToken(pair.Token);
            var userId = GetUserId(jwt);
            var sessionId = GetSessionId(jwt);
            await _sessionService.StoreSessionAsync(userId, sessionId, clientInfo, pair.RefreshToken, pair.RefreshTokenExpiryTime);
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

        return await RequestTokenAsync(request, (pair, clientInfo) =>
        {
            var jwt = ParseToken(pair.Token);
            var tokenUserId = GetUserId(jwt);
            var sessionId = GetSessionId(jwt);
            return _sessionService.StoreSessionAsync(tokenUserId, sessionId, clientInfo, pair.RefreshToken, pair.RefreshTokenExpiryTime);
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
            var jwt = ParseToken(pair.Token);
            var userId = GetUserId(jwt);
            var sessionId = GetSessionId(jwt);

            var oldRefreshHash = TokenUtilities.HashToken(refreshToken, _options.Secret);
            var info = await _sessionService.GetRefreshTokenInfoAsync(oldRefreshHash);
            if (info != null && info.Value.UserId == userId)
            {
                var newRefreshHash = TokenUtilities.HashToken(pair.RefreshToken, _options.Secret);
                await _sessionService.RotateRefreshTokenAsync(info.Value.RefreshTokenId, newRefreshHash, pair.RefreshTokenExpiryTime);
            }
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

    private JwtSecurityToken ParseToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key)),
            ValidateIssuer = true,
            ValidIssuer = _jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = _jwtOptions.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            handler.ValidateToken(token, parameters, out var validatedToken);
            return (JwtSecurityToken)validatedToken;
        }
        catch (Exception ex) when (ex is SecurityTokenException || ex is ArgumentException)
        {
            throw new SecurityTokenException($"Token validation failed: {ex.Message}", ex);
        }
    }

    private static string GetUserId(JwtSecurityToken jwt) => jwt.Subject ?? string.Empty;

    private static Guid GetSessionId(JwtSecurityToken jwt)
    {
        var sid = jwt.Claims.FirstOrDefault(c => c.Type == AuthConstants.Claims.SessionId)?.Value;
        return Guid.TryParse(sid, out var id) ? id : Guid.Empty;
    }
}

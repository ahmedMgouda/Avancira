using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using Avancira.Application.Common;
using Avancira.Application.Identity;
using Avancira.Domain.Identity;
using Avancira.Infrastructure.Persistence;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Avancira.Application.Auth.Jwt;
using Microsoft.Extensions.Options;
using Avancira.Infrastructure.Identity.Users;
using Avancira.Application.Identity.Tokens.Dtos;
using OpenIddict.Abstractions;

namespace Avancira.Infrastructure.Auth;

public class AuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly IClientInfoService _clientInfoService;
    private readonly AvanciraDbContext _dbContext;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly JwtOptions _jwtOptions;

    public AuthenticationService(
        IHttpClientFactory httpClientFactory,
        IClientInfoService clientInfoService,
        AvanciraDbContext dbContext,
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IOptions<JwtOptions> jwtOptions)
    {
        _httpClient = httpClientFactory.CreateClient();
        _clientInfoService = clientInfoService;
        _dbContext = dbContext;
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<TokenPair> ExchangeCodeAsync(string code, string codeVerifier, string redirectUri)
    {
        var clientInfo = await _clientInfoService.GetClientInfoAsync();

        var content = new FormUrlEncodedContent(new Dictionary<string, string?>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["code_verifier"] = codeVerifier,
            ["scope"] = $"api {OpenIddictConstants.Scopes.OfflineAccess}",
            ["device_id"] = clientInfo.DeviceId
        });

        var response = await _httpClient.PostAsync("/connect/token", content);
        response.EnsureSuccessStatusCode();

        return await HandleTokenResponseAsync(response, clientInfo, string.Empty);
    }

    public async Task<TokenPair?> PasswordSignInAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return null;
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, password, false);
        if (!result.Succeeded)
        {
            return null;
        }

        return await GenerateTokenAsync(user.Id);
    }

    public async Task<TokenPair> GenerateTokenAsync(string userId)
    {
        var clientInfo = await _clientInfoService.GetClientInfoAsync();

        var content = new FormUrlEncodedContent(new Dictionary<string, string?>
        {
            ["grant_type"] = "user_id",
            ["user_id"] = userId,
            ["scope"] = "api offline_access",
            ["device_id"] = clientInfo.DeviceId
        });

        var response = await _httpClient.PostAsync("/connect/token", content);
        response.EnsureSuccessStatusCode();

        return await HandleTokenResponseAsync(response, clientInfo, userId);
    }

    public async Task<TokenPair> RefreshTokenAsync(string refreshToken)
    {
        var clientInfo = await _clientInfoService.GetClientInfoAsync();

        var content = new FormUrlEncodedContent(new Dictionary<string, string?>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["device_id"] = clientInfo.DeviceId
        });

        var response = await _httpClient.PostAsync("/connect/token", content);
        response.EnsureSuccessStatusCode();

        var (token, newRefresh, refreshExpiry) = await ParseTokenResponseAsync(response);
        return new TokenPair(token, newRefresh, refreshExpiry);
    }

    private async Task<TokenPair> HandleTokenResponseAsync(HttpResponseMessage response, ClientInfo clientInfo, string userId)
    {
        var (token, refresh, refreshExpiry) = await ParseTokenResponseAsync(response);

        if (string.IsNullOrEmpty(userId))
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            userId = jwt.Subject;
        }

        var existingSession = await _dbContext.Sessions
            .Include(s => s.RefreshTokens)
            .SingleOrDefaultAsync(s => s.UserId == userId && s.Device == clientInfo.DeviceId);
        await using var tx = await _dbContext.Database.BeginTransactionAsync();

        if (existingSession != null)
        {
            _dbContext.RefreshTokens.RemoveRange(existingSession.RefreshTokens);
            _dbContext.Sessions.Remove(existingSession);
        }

        var now = DateTime.UtcNow;
        var session = new Session
        {
            UserId = userId,
            Device = clientInfo.DeviceId,
            UserAgent = clientInfo.UserAgent,
            OperatingSystem = clientInfo.OperatingSystem,
            IpAddress = clientInfo.IpAddress,
            Country = clientInfo.Country,
            City = clientInfo.City,
            CreatedUtc = now,
            LastActivityUtc = now,
            LastRefreshUtc = now,
            AbsoluteExpiryUtc = refreshExpiry
        };

        session.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = TokenUtilities.HashToken(refresh),
            CreatedUtc = now,
            AbsoluteExpiryUtc = refreshExpiry
        });

        _dbContext.Sessions.Add(session);
        await _dbContext.SaveChangesAsync();
        await tx.CommitAsync();

        return new TokenPair(token, refresh, refreshExpiry);
    }

    private async Task<(string Token, string RefreshToken, DateTime RefreshExpiry)> ParseTokenResponseAsync(HttpResponseMessage response)
    {
        using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var root = document.RootElement;

        var token = root.GetProperty("access_token").GetString() ?? string.Empty;
        var refresh = root.GetProperty("refresh_token").GetString() ?? string.Empty;
        DateTime refreshExpiry = DateTime.UtcNow;
        if (root.TryGetProperty("refresh_token_expires_in", out var exp))
        {
            refreshExpiry = DateTime.UtcNow.AddSeconds(exp.GetInt32());
        }
        else
        {
            refreshExpiry = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDefaultDays);
        }

        return (token, refresh, refreshExpiry);
    }

}


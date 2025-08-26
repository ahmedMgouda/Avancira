using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using Avancira.Application.Common;
using Avancira.Application.Identity;
using Avancira.Domain.Identity;
using Avancira.Infrastructure.Persistence;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Avancira.Infrastructure.Auth;

public class AuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly IClientInfoService _clientInfoService;
    private readonly AvanciraDbContext _dbContext;

    public AuthenticationService(IHttpClientFactory httpClientFactory, IClientInfoService clientInfoService, AvanciraDbContext dbContext)
    {
        _httpClient = httpClientFactory.CreateClient();
        _clientInfoService = clientInfoService;
        _dbContext = dbContext;
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
            ["device_id"] = clientInfo.DeviceId
        });

        var response = await _httpClient.PostAsync("/connect/token", content);
        response.EnsureSuccessStatusCode();

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
            refreshExpiry = DateTime.UtcNow.AddDays(7);
        }

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var userId = jwt.Subject;

        var existingSession = await _dbContext.Sessions
            .Include(s => s.RefreshTokens)
            .SingleOrDefaultAsync(s => s.UserId == userId && s.Device == clientInfo.DeviceId);

        if (existingSession != null)
        {
            _dbContext.RefreshTokens.RemoveRange(existingSession.RefreshTokens);
            _dbContext.Sessions.Remove(existingSession);
            await _dbContext.SaveChangesAsync();
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
            TokenHash = HashToken(refresh),
            CreatedUtc = now,
            AbsoluteExpiryUtc = refreshExpiry
        });

        _dbContext.Sessions.Add(session);
        await _dbContext.SaveChangesAsync();

        return new TokenPair(token, refresh, refreshExpiry);
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
            refreshExpiry = DateTime.UtcNow.AddDays(7);
        }

        var existingSession = await _dbContext.Sessions
            .Include(s => s.RefreshTokens)
            .SingleOrDefaultAsync(s => s.UserId == userId && s.Device == clientInfo.DeviceId);

        if (existingSession != null)
        {
            _dbContext.RefreshTokens.RemoveRange(existingSession.RefreshTokens);
            _dbContext.Sessions.Remove(existingSession);
            await _dbContext.SaveChangesAsync();
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
            TokenHash = HashToken(refresh),
            CreatedUtc = now,
            AbsoluteExpiryUtc = refreshExpiry
        });

        _dbContext.Sessions.Add(session);
        await _dbContext.SaveChangesAsync();

        return new TokenPair(token, refresh, refreshExpiry);
    }

    private static string HashToken(string token)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}


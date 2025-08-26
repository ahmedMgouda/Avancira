using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using Avancira.Application.Common;
using Avancira.Application.Identity;
using Avancira.Application.Identity.Tokens.Dtos;
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

    public async Task<TokenPair> GenerateTokenAsync(TokenGenerationDto request)
    {
        var clientInfo = await _clientInfoService.GetClientInfoAsync();

        var content = new FormUrlEncodedContent(new Dictionary<string, string?>
        {
            ["grant_type"] = "password",
            ["username"] = request.Email,
            ["password"] = request.Password,
            ["scope"] = "api offline_access"
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

    public async Task<TokenPair> RefreshTokenAsync(string refreshToken)
    {
        var clientInfo = await _clientInfoService.GetClientInfoAsync();

        var content = new FormUrlEncodedContent(new Dictionary<string, string?>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken
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

        var now = DateTime.UtcNow;
        var hash = HashToken(refreshToken);
        var existingToken = await _dbContext.RefreshTokens
            .Include(r => r.Session)
            .FirstOrDefaultAsync(r => r.TokenHash == hash);

        if (existingToken != null)
        {
            existingToken.RevokedUtc = now;
            var session = existingToken.Session;
            session.LastRefreshUtc = now;
            session.LastActivityUtc = now;
            session.IpAddress = clientInfo.IpAddress;
            session.UserAgent = clientInfo.UserAgent;
            session.OperatingSystem = clientInfo.OperatingSystem;
            session.Country = clientInfo.Country;
            session.City = clientInfo.City;

            _dbContext.RefreshTokens.Add(new RefreshToken
            {
                TokenHash = HashToken(refresh),
                SessionId = session.Id,
                RotatedFromId = existingToken.Id,
                CreatedUtc = now,
                AbsoluteExpiryUtc = refreshExpiry
            });

            await _dbContext.SaveChangesAsync();
        }

        return new TokenPair(token, refresh, refreshExpiry);
    }

    private static string HashToken(string token)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}


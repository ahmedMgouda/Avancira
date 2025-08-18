using Avancira.Application.Auth.Jwt;
using Avancira.Application.Common;
using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Tokens.Dtos;
using Avancira.Domain.Common.Exceptions;
using Avancira.Infrastructure.Auth.Jwt;
using Avancira.Infrastructure.Identity.Audit;
using Avancira.Infrastructure.Identity.Users;
using Avancira.Infrastructure.Persistence;
using Avancira.Shared.Authorization;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using UAParser;

namespace Avancira.Infrastructure.Identity.Tokens;
public sealed class TokenService : ITokenService
{
    private readonly UserManager<User> _userManager;
    private readonly JwtOptions _jwtOptions;
    private readonly IPublisher _publisher;
    private readonly AvanciraDbContext _dbContext;

    public TokenService(IOptions<JwtOptions> jwtOptions, UserManager<User> userManager, IPublisher publisher, AvanciraDbContext dbContext)
    {
        _jwtOptions = jwtOptions.Value;
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _publisher = publisher;
        _dbContext = dbContext;
    }

    public async Task<TokenPair> GenerateTokenAsync(TokenGenerationDto request, string deviceId, ClientInfo clientInfo, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim().Normalize());
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedException();
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedException("user is deactivated");
        }

        if (!user.EmailConfirmed)
        {
            throw new UnauthorizedException("email not confirmed");
        }

        return await GenerateTokens(user, deviceId, clientInfo, cancellationToken);
    }

    public async Task<TokenPair> RefreshTokenAsync(string? token, string refreshToken, string deviceId, ClientInfo clientInfo, CancellationToken cancellationToken)
    {
        var hashedRefreshToken = HashToken(refreshToken);
        var tokenEntity = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(t => t.Device == deviceId && t.TokenHash == hashedRefreshToken && !t.Revoked, cancellationToken);
        if (tokenEntity is null || tokenEntity.ExpiresAt <= DateTime.UtcNow)
        {
            throw new UnauthorizedException("Invalid Refresh Token");
        }

        if (!string.IsNullOrWhiteSpace(token))
        {
            var principal = GetPrincipalFromExpiredToken(token);
            var userIdFromToken = _userManager.GetUserId(principal)!;
            if (userIdFromToken != tokenEntity.UserId)
            {
                throw new UnauthorizedException();
            }
        }

        var user = await _userManager.FindByIdAsync(tokenEntity.UserId);
        if (user is null)
        {
            throw new UnauthorizedException();
        }

        tokenEntity.Revoked = true;
        tokenEntity.RevokedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GenerateTokens(user, deviceId, clientInfo, cancellationToken);
    }

    public async Task RevokeTokenAsync(string refreshToken, string userId, string deviceId, CancellationToken cancellationToken)
    {
        var hashedToken = HashToken(refreshToken);
        var token = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(t => t.UserId == userId && t.Device == deviceId && t.TokenHash == hashedToken && !t.Revoked, cancellationToken);

        if (token is null)
        {
            throw new UnauthorizedException("Invalid Refresh Token");
        }

        token.Revoked = true;
        token.RevokedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(new AuditPublishedEvent(new()
        {
            new()
            {
                Id = Guid.NewGuid(),
                Operation = "Token Revoked",
                Entity = "Identity",
                UserId = new Guid(userId),
                DateTime = DateTime.UtcNow,
            }
        }));
    }

    private async Task<TokenPair> GenerateTokens(User user, string deviceId, ClientInfo clientInfo, CancellationToken cancellationToken)
    {
        string token = await GenerateJwt(user, deviceId, clientInfo.IpAddress);

        var client = Parser.GetDefault().Parse(clientInfo.UserAgent);
        var browser = $"{client.UA.Family} {client.UA.Major}".Trim();
        var os = client.OS.ToString();
        if (browser.Length > 100) browser = browser[..100];
        if (os.Length > 100) os = os[..100];

        var refreshToken = GenerateRefreshToken();
        var refreshTokenHash = HashToken(refreshToken);
        var refreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationInDays);

        var oldTokens = await _dbContext.RefreshTokens
            .Where(t => t.UserId == user.Id && t.Device == deviceId && !t.Revoked)
            .ToListAsync(cancellationToken);
        foreach (var oldToken in oldTokens)
        {
            oldToken.Revoked = true;
            oldToken.RevokedAt = DateTime.UtcNow;
        }

        var country = clientInfo.Country;
        var city = clientInfo.City;

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            Device = deviceId,
            UserAgent = browser,
            OperatingSystem = os,
            IpAddress = clientInfo.IpAddress,
            Country = country,
            City = city,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = refreshTokenExpiryTime,
            Revoked = false
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(new AuditPublishedEvent(new()
        {
            new()
            {
                Id = Guid.NewGuid(),
                Operation = "Token Generated",
                Entity = "Identity",
                UserId = new Guid(user.Id),
                DateTime = DateTime.UtcNow,
            }
        }));

        return new TokenPair(token, refreshToken, refreshTokenExpiryTime);
    }

    private async Task<string> GenerateJwt(User user, string deviceId, string ipAddress) =>
        GenerateEncryptedToken(GetSigningCredentials(), await GetClaimsAsync(user, deviceId, ipAddress));

    private SigningCredentials GetSigningCredentials()
    {
        byte[] secret = Encoding.UTF8.GetBytes(_jwtOptions.Key);
        return new SigningCredentials(new SymmetricSecurityKey(secret), SecurityAlgorithms.HmacSha256);
    }

    private string GenerateEncryptedToken(SigningCredentials signingCredentials, IEnumerable<Claim> claims)
    {
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.TokenExpirationInMinutes),
            signingCredentials: signingCredentials,
            issuer: JwtAuthConstants.Issuer,
            audience: JwtAuthConstants.Audience
        );
        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(token);
    }

    private async Task<List<Claim>> GetClaimsAsync(User user, string deviceId, string ipAddress)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.GivenName, user.FirstName ?? string.Empty),
            new(ClaimTypes.Surname, user.LastName ?? string.Empty),
            new(AvanciraClaims.Fullname, $"{user.FirstName} {user.LastName}"),
            new(ClaimTypes.MobilePhone, user.PhoneNumber ?? string.Empty),
            new(AvanciraClaims.TimeZoneId, user.TimeZoneId ?? string.Empty),
            new(AvanciraClaims.IpAddress, ipAddress),
            new(AvanciraClaims.ImageUrl, user.ImageUrl == null ? string.Empty : user.ImageUrl.ToString())
        };

        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        return claims;
    }

    private static string GenerateRefreshToken()
    {
        byte[] randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudience = JwtAuthConstants.Audience,
            ValidIssuer = JwtAuthConstants.Issuer,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.Zero,
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedException("invalid token");
        }

        return principal;
    }
}

using Avancira.Application.Auth.Jwt;
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

    public async Task<TokenResponse> GenerateTokenAsync(TokenGenerationDto request, string deviceId, string ipAddress, CancellationToken cancellationToken)
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

        return await GenerateTokens(user, deviceId, ipAddress);
    }

    public async Task<TokenResponse> RefreshTokenAsync(RefreshTokenDto request, string deviceId, string ipAddress, CancellationToken cancellationToken)
    {
        var userPrincipal = GetPrincipalFromExpiredToken(request.Token);
        var userId = _userManager.GetUserId(userPrincipal)!;
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            throw new UnauthorizedException();
        }

        var hashedRequestToken = HashToken(request.RefreshToken);
        var refreshToken = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(t => t.UserId == userId && t.Device == deviceId && t.TokenHash == hashedRequestToken, cancellationToken);
        if (refreshToken is null || refreshToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new UnauthorizedException("Invalid Refresh Token");
        }

        _dbContext.RefreshTokens.Remove(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GenerateTokens(user, deviceId, ipAddress);
    }

    private async Task<TokenResponse> GenerateTokens(User user, string deviceId, string ipAddress)
    {
        string token = await GenerateJwt(user, deviceId, ipAddress);

        var refreshToken = GenerateRefreshToken();
        var refreshTokenHash = HashToken(refreshToken);
        var refreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationInDays);

        var oldTokens = await _dbContext.RefreshTokens
            .Where(t => t.UserId == user.Id && t.Device == deviceId)
            .ToListAsync();
        if (oldTokens.Count > 0)
        {
            _dbContext.RefreshTokens.RemoveRange(oldTokens);
        }

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            Device = deviceId,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = refreshTokenExpiryTime,
            Revoked = false
        });

        await _dbContext.SaveChangesAsync();

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

        return new TokenResponse(token, refreshToken, refreshTokenExpiryTime);
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
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.FirstName ?? string.Empty),
            new(ClaimTypes.MobilePhone, user.PhoneNumber ?? string.Empty),
            new(AvanciraClaims.Fullname, $"{user.FirstName} {user.LastName}"),
            new(ClaimTypes.Surname, user.LastName ?? string.Empty),
            new(AvanciraClaims.TimeZoneId, user.TimeZoneId ?? string.Empty),
            new(AvanciraClaims.IpAddress, ipAddress),
            new(AvanciraClaims.DeviceId, deviceId),
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

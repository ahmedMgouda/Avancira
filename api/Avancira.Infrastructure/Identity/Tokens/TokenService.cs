using Avancira.Application.Auth.Jwt;
using Avancira.Application.Common;
using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Tokens.Dtos;
using Avancira.Domain.Common.Exceptions;
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
using System.Linq;

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

    public async Task<TokenPair> GenerateTokenAsync(TokenGenerationDto request, ClientInfo clientInfo, CancellationToken cancellationToken)
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

        return await GenerateTokens(user, clientInfo, cancellationToken);
    }

    public async Task<TokenPair> RefreshTokenAsync(string? token, string refreshToken, ClientInfo clientInfo, CancellationToken cancellationToken)
    {
        var hashedRefreshToken = HashToken(refreshToken);
        var tokenEntity = await _dbContext.RefreshTokens
            .Include(t => t.Session)
            .SingleOrDefaultAsync(t => t.Session.Device == clientInfo.DeviceId && t.TokenHash == hashedRefreshToken && t.RevokedUtc == null, cancellationToken);

        if (tokenEntity is null)
        {
            throw new UnauthorizedException("Invalid Refresh Token");
        }

        var session = tokenEntity.Session;
        if (session.RevokedUtc != null || session.AbsoluteExpiryUtc <= DateTime.UtcNow)
        {
            throw new UnauthorizedException("Invalid Refresh Token");
        }

        if (!string.IsNullOrWhiteSpace(token))
        {
            var principal = GetPrincipalFromExpiredToken(token);
            var userIdFromToken = _userManager.GetUserId(principal)!;
            if (userIdFromToken != session.UserId)
            {
                throw new UnauthorizedException();
            }
        }

        var user = await _userManager.FindByIdAsync(session.UserId);
        if (user is null)
        {
            throw new UnauthorizedException();
        }

        string accessToken = await GenerateJwt(user, clientInfo.DeviceId, clientInfo.IpAddress);
        string newRefreshToken = GenerateRefreshToken();
        string newRefreshTokenHash = HashToken(newRefreshToken);
        var refreshTokenExpiryTime = session.AbsoluteExpiryUtc;

        tokenEntity.RevokedUtc = DateTime.UtcNow;

        var rotated = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = newRefreshTokenHash,
            SessionId = session.Id,
            RotatedFromId = tokenEntity.Id,
            CreatedUtc = DateTime.UtcNow,
            RevokedUtc = null
        };

        _dbContext.RefreshTokens.Add(rotated);

        session.LastRefreshUtc = DateTime.UtcNow;
        session.LastActivityUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(new AuditPublishedEvent(new()
        {
            new()
            {
                Id = Guid.NewGuid(),
                Operation = "Token Refreshed",
                Entity = "Identity",
                UserId = new Guid(session.UserId),
                DateTime = DateTime.UtcNow,
            }
        }));

        return new TokenPair(accessToken, newRefreshToken, refreshTokenExpiryTime);
    }

    public async Task RevokeTokenAsync(string refreshToken, string userId, ClientInfo clientInfo, CancellationToken cancellationToken)
    {
        var hashedToken = HashToken(refreshToken);
        var session = await _dbContext.Sessions
            .Include(s => s.RefreshTokens)
            .SingleOrDefaultAsync(s => s.UserId == userId && s.Device == clientInfo.DeviceId, cancellationToken);

        if (session is null || session.RefreshTokens.All(t => t.TokenHash != hashedToken))
        {
            throw new UnauthorizedException("Invalid Refresh Token");
        }

        session.RevokedUtc = DateTime.UtcNow;
        foreach (var t in session.RefreshTokens.Where(t => t.RevokedUtc == null))
        {
            t.RevokedUtc = DateTime.UtcNow;
        }
        session.LastActivityUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(new AuditPublishedEvent(new()
        {
            new()
            {
                Id = Guid.NewGuid(),
                Operation = "Session Revoked",
                Entity = "Identity",
                UserId = new Guid(userId),
                DateTime = DateTime.UtcNow,
            }
        }));
    }

    public async Task<IReadOnlyList<SessionDto>> GetSessionsAsync(string userId, CancellationToken ct)
    {
        return await _dbContext.Sessions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.RevokedUtc == null && s.AbsoluteExpiryUtc > DateTime.UtcNow)
            .OrderByDescending(s => s.LastActivityUtc)
            .Select(s => new SessionDto(
                s.Id,
                s.Device,
                s.UserAgent,
                s.OperatingSystem,
                s.IpAddress,
                s.Country,
                s.City,
                s.CreatedUtc,
                s.LastActivityUtc,
                s.AbsoluteExpiryUtc,
                s.RevokedUtc))
            .ToListAsync(ct);
    }

    public async Task RevokeSessionAsync(Guid sessionId, string userId, CancellationToken ct)
    {
        var session = await _dbContext.Sessions
            .Include(s => s.RefreshTokens)
            .SingleOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, ct);

        if (session is null)
        {
            throw new UnauthorizedException("Invalid Refresh Token");
        }

        session.RevokedUtc = DateTime.UtcNow;
        foreach (var token in session.RefreshTokens.Where(t => t.RevokedUtc == null))
        {
            token.RevokedUtc = DateTime.UtcNow;
        }
        session.LastActivityUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);

        await _publisher.Publish(new AuditPublishedEvent(new()
        {
            new()
            {
                Id = Guid.NewGuid(),
                Operation = "Session Revoked",
                Entity = "Identity",
                UserId = new Guid(userId),
                DateTime = DateTime.UtcNow,
            }
        }));
    }

    public async Task UpdateSessionActivityAsync(string userId, string deviceId, CancellationToken ct)
    {
        var session = await _dbContext.Sessions
            .SingleOrDefaultAsync(s => s.UserId == userId && s.Device == deviceId && s.RevokedUtc == null, ct);

        if (session is null || session.AbsoluteExpiryUtc <= DateTime.UtcNow)
        {
            return;
        }

        session.LastActivityUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);
    }

    private async Task<TokenPair> GenerateTokens(User user, ClientInfo clientInfo, CancellationToken cancellationToken)
    {
        string token = await GenerateJwt(user, clientInfo.DeviceId, clientInfo.IpAddress);

        var refreshToken = GenerateRefreshToken();
        var refreshTokenHash = HashToken(refreshToken);

        var session = await _dbContext.Sessions
            .Include(s => s.RefreshTokens)
            .SingleOrDefaultAsync(s => s.UserId == user.Id && s.Device == clientInfo.DeviceId && s.RevokedUtc == null, cancellationToken);

        if (session is null || session.AbsoluteExpiryUtc <= DateTime.UtcNow)
        {
            var newExpiry = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationInDays);
            if (session is null)
            {
                session = new Session
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Device = clientInfo.DeviceId,
                    UserAgent = clientInfo.UserAgent,
                    OperatingSystem = clientInfo.OperatingSystem,
                    IpAddress = clientInfo.IpAddress,
                    Country = clientInfo.Country,
                    City = clientInfo.City,
                    CreatedUtc = DateTime.UtcNow,
                    AbsoluteExpiryUtc = newExpiry,
                    LastRefreshUtc = DateTime.UtcNow,
                    LastActivityUtc = DateTime.UtcNow
                };
                _dbContext.Sessions.Add(session);
            }
            else
            {
                foreach (var tk in session.RefreshTokens.Where(t => t.RevokedUtc == null))
                {
                    tk.RevokedUtc = DateTime.UtcNow;
                }
                session.CreatedUtc = DateTime.UtcNow;
                session.AbsoluteExpiryUtc = newExpiry;
                session.RevokedUtc = null;
            }
        }
        else
        {
            session.UserAgent = clientInfo.UserAgent;
            session.OperatingSystem = clientInfo.OperatingSystem;
            session.IpAddress = clientInfo.IpAddress;
            session.Country = clientInfo.Country;
            session.City = clientInfo.City;
        }

        session.LastRefreshUtc = DateTime.UtcNow;
        session.LastActivityUtc = DateTime.UtcNow;

        var refreshTokenExpiryTime = session.AbsoluteExpiryUtc;

        var previousToken = session.RefreshTokens
            .Where(t => t.RevokedUtc == null)
            .OrderByDescending(t => t.CreatedUtc)
            .FirstOrDefault();

        if (previousToken != null)
        {
            previousToken.RevokedUtc = DateTime.UtcNow;
        }

        var newToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = refreshTokenHash,
            SessionId = session.Id,
            RotatedFromId = previousToken?.Id,
            CreatedUtc = DateTime.UtcNow,
            RevokedUtc = null
        };

        _dbContext.RefreshTokens.Add(newToken);

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
        GenerateSignedToken(GetSigningCredentials(), await GetClaimsAsync(user, deviceId, ipAddress));

    private SigningCredentials GetSigningCredentials()
    {
        byte[] secret = Encoding.UTF8.GetBytes(_jwtOptions.Key);
        return new SigningCredentials(new SymmetricSecurityKey(secret), SecurityAlgorithms.HmacSha256);
    }

    /// <summary>
    /// Generates a signed JWT token using the provided credentials and claims.
    /// </summary>
    /// <param name="signingCredentials">Credentials used to sign the token.</param>
    /// <param name="claims">Claims to embed in the JWT.</param>
    /// <returns>The serialized JWT token.</returns>
    private string GenerateSignedToken(SigningCredentials signingCredentials, IEnumerable<Claim> claims)
    {
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.TokenExpirationInMinutes),
            signingCredentials: signingCredentials,
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience
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
            ValidAudience = _jwtOptions.Audience,
            ValidIssuer = _jwtOptions.Issuer,
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

using Avancira.Application.Catalog;
using Avancira.Application.Catalog.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Avancira.Infrastructure.Catalog
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly ILogger<JwtTokenService> _logger;
        private readonly string _jwtSecret;
        private readonly string _jitsiAppId;

        public JwtTokenService(
            ILogger<JwtTokenService> logger,
            IConfiguration configuration
        )
        {
            _logger = logger;
            _jwtSecret = configuration["Jwt:Secret"] ?? "your-super-secret-jwt-key-that-should-be-at-least-256-bits";
            _jitsiAppId = configuration["Jitsi:AppId"] ?? "avancira";
        }

        public Meeting GetMeeting(string userName, string roomName)
        {
            try
            {
                var meetingToken = GenerateJitsiToken(userName, roomName);
                var meetingUrl = $"https://meet.jit.si/{roomName}?jwt={meetingToken}";

                _logger.LogInformation("Generated meeting for user {UserName} in room {RoomName}", userName, roomName);

                return new Meeting
                {
                    Token = meetingToken,
                    MeetingUrl = meetingUrl,
                    RoomName = roomName,
                    UserName = userName,
                    Domain = "meet.jit.si",
                    ServerUrl = "https://meet.jit.si"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating meeting for user {UserName} in room {RoomName}", userName, roomName);
                throw;
            }
        }

        private string GenerateJitsiToken(string userName, string roomName)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_jwtSecret);

                var claims = new[]
                {
                    new Claim("iss", _jitsiAppId),
                    new Claim("sub", _jitsiAppId),
                    new Claim("aud", "jitsi"),
                    new Claim("room", roomName),
                    new Claim("context", $"{{\"user\":{{\"name\":\"{userName}\"}},\"group\":\"\"}}", JsonClaimValueTypes.Json),
                    new Claim("moderator", "true"), // Set to true to allow moderator privileges
                    new Claim("exp", ((DateTimeOffset)DateTime.UtcNow.AddHours(2)).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                    new Claim("nbf", ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                    new Claim("iat", ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
                };

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddHours(2),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                    Issuer = _jitsiAppId,
                    Audience = "jitsi"
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                return tokenHandler.WriteToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Jitsi JWT token");
                throw;
            }
        }
    }
}

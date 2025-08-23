using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Avancira.Application.Auth;
using Avancira.Application.Common;
using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Tokens.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[Route("api/auth")]
public class ExternalAuthController : BaseApiController
{
    private readonly IExternalAuthService _externalAuthService;
    private readonly IExternalUserService _externalUserService;
    private readonly ITokenService _tokenService;
    private readonly IClientInfoService _clientInfoService;

    public ExternalAuthController(
        IExternalAuthService externalAuthService,
        IExternalUserService externalUserService,
        ITokenService tokenService,
        IClientInfoService clientInfoService)
    {
        _externalAuthService = externalAuthService;
        _externalUserService = externalUserService;
        _tokenService = tokenService;
        _clientInfoService = clientInfoService;
    }

    [HttpPost("external-login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExternalLogin([FromBody] ExternalLoginRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _externalAuthService.ValidateTokenAsync(request.Provider, request.Token);

        if (!result.Succeeded || result.LoginInfo is null)
        {
            return result.ErrorType switch
            {
                ExternalAuthErrorType.UnsupportedProvider => BadRequest(result.Error),
                _ => Unauthorized(result.Error)
            };
        }

        var info = result.LoginInfo;

        var userResult = await _externalUserService.EnsureUserAsync(info);
        if (!userResult.Succeeded)
        {
            return userResult.ErrorType switch
            {
                ExternalUserError.Unauthorized => Unauthorized(),
                ExternalUserError.BadRequest => BadRequest(userResult.Error),
                ExternalUserError.Problem => Problem(userResult.Error),
                _ => Problem(userResult.Error)
            };
        }

        var clientInfo = await _clientInfoService.GetClientInfoAsync();
        var tokens = await _tokenService.GenerateTokenForUserAsync(userResult.UserId!, clientInfo, cancellationToken);

        SetRefreshTokenCookie(tokens.RefreshToken, tokens.RefreshTokenExpiryTime);

        return Ok(new TokenResponse(tokens.Token));
    }

    public class ExternalLoginRequest : IValidatableObject
    {
        [Required]
        public SocialProvider Provider { get; set; }

        [Required]
        public string Token { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var authService = validationContext.GetService(typeof(IExternalAuthService)) as IExternalAuthService;
            if (authService != null && !authService.SupportsProvider(Provider))
            {
                yield return new ValidationResult("Unsupported provider", new[] { nameof(Provider) });
            }
        }
    }
}

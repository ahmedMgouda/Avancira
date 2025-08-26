using Avancira.Application.Identity.Tokens.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Collections.Generic;

namespace Avancira.API.Controllers;

[Route("api/auth")]
public class AuthController : BaseApiController
{
    private readonly HttpClient _httpClient;

    public AuthController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    [HttpPost("token")]
    [AllowAnonymous]
    public async Task<IActionResult> GenerateToken([FromBody] TokenGenerationDto request)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = request.Email,
            ["password"] = request.Password
        });

        var response = await _httpClient.PostAsync("/connect/token", content);
        var body = await response.Content.ReadAsStringAsync();
        return Content(body, "application/json");
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto? request)
    {
        var refreshToken = request?.Token ?? Request.Cookies["refreshToken"];
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized();
        }

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken
        });

        var response = await _httpClient.PostAsync("/connect/token", content);
        var body = await response.Content.ReadAsStringAsync();
        return Content(body, "application/json");
    }
}

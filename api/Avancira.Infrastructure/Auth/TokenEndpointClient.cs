using System.Net;
using System.Net.Http;
using Avancira.Application.Identity.Tokens.Dtos;
using Avancira.Domain.Common.Exceptions;

namespace Avancira.Infrastructure.Auth;

public class TokenEndpointClient : ITokenEndpointClient
{
    private readonly HttpClient _httpClient;
    private readonly ITokenResponseParser _parser;

    public TokenEndpointClient(IHttpClientFactory httpClientFactory, ITokenResponseParser parser)
    {
        _httpClient = httpClientFactory.CreateClient("TokenClient");
        _parser = parser;
    }

    public async Task<TokenPair> RequestTokenAsync(TokenRequestBuilder builder)
    {
        var content = builder.Build();
        using var response = await _httpClient.PostAsync(AuthConstants.Endpoints.Token, content);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedException();
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new TokenRequestException(response.StatusCode);
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        return await _parser.ParseAsync(stream);
    }
}


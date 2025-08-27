using System.Net.Http;
using Avancira.Application.Identity.Tokens.Dtos;

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

    public async Task<TokenPair> RequestTokenAsync(TokenRequestParams parameters)
    {
        var content = TokenRequestBuilder.Build(parameters);
        using var response = await _httpClient.PostAsync(AuthConstants.Endpoints.Token, content);

        await using var stream = await response.Content.ReadAsStreamAsync();
        return await _parser.ParseAsync(stream);
    }
}

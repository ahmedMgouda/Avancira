using Avancira.Application.Identity.Tokens.Dtos;

namespace Avancira.Infrastructure.Auth;

public interface ITokenEndpointClient
{
    Task<TokenPair> RequestTokenAsync(TokenRequestBuilder builder);
}


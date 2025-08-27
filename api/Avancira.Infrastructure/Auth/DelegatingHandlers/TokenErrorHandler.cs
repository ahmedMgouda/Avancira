using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Avancira.Domain.Common.Exceptions;

namespace Avancira.Infrastructure.Auth.DelegatingHandlers;

public class TokenErrorHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedException();
        }

        if (!response.IsSuccessStatusCode)
        {
            var problem = await response.Content.ReadFromJsonAsync<TokenRequestProblemDetails>(cancellationToken: cancellationToken);
            throw new TokenRequestException(problem?.Error, problem?.ErrorDescription, response.StatusCode);
        }

        return response;
    }
}

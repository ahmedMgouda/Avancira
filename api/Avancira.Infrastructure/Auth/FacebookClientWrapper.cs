using System.Text.Json;
using Facebook;

namespace Avancira.Infrastructure.Auth;

public interface IFacebookClient
{
    Task<JsonDocument> GetAsync(string path, IDictionary<string, object> parameters);
}

public class FacebookClientWrapper : IFacebookClient
{
    private readonly FacebookClient _client = new();

    public Task<JsonDocument> GetAsync(string path, IDictionary<string, object> parameters)
        => _client.GetTaskAsync<JsonDocument>(path, parameters);
}

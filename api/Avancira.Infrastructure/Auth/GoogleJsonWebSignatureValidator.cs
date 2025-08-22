using Google.Apis.Auth;

namespace Avancira.Infrastructure.Auth;

public interface IGoogleJsonWebSignatureValidator
{
    Task<GoogleJsonWebSignature.Payload> ValidateAsync(string idToken, string clientId);
}

public class GoogleJsonWebSignatureValidator : IGoogleJsonWebSignatureValidator
{
    public Task<GoogleJsonWebSignature.Payload> ValidateAsync(string idToken, string clientId)
    {
        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { clientId }
        };
        return GoogleJsonWebSignature.ValidateAsync(idToken, settings);
    }
}

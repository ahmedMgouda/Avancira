using Avancira.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Linq;

namespace Avancira.API.Controllers;

[AllowAnonymous]
[Route("api/configs")]
public class ConfigsController : BaseApiController
{
    private readonly StripeOptions _stripeOptions;
    private readonly PayPalOptions _payPalOptions;
    private readonly GoogleOptions _googleOptions;
    private readonly FacebookOptions _facebookOptions;
    private readonly IEnumerable<IExternalTokenValidator> _tokenValidators;

    public ConfigsController(
        IOptions<StripeOptions> stripeOptions,
        IOptions<PayPalOptions> payPalOptions,
        IOptions<GoogleOptions> googleOptions,
        IOptions<FacebookOptions> facebookOptions,
        IEnumerable<IExternalTokenValidator> tokenValidators
    )
    {
        _stripeOptions = stripeOptions.Value;
        _payPalOptions = payPalOptions.Value;
        _googleOptions = googleOptions.Value;
        _facebookOptions = facebookOptions.Value;
        _tokenValidators = tokenValidators;
    }

    // Read
    [HttpGet]
    public IActionResult GetConfig()
    {
        var config = new
        {
            stripePublishableKey = _stripeOptions.PublishableKey,
            payPalClientId = _payPalOptions.ClientId,
            googleMapsApiKey = _googleOptions.ApiKey,
            googleClientId = _googleOptions.ClientId,
            facebookAppId = _facebookOptions.AppId,
            enabledSocialProviders = _tokenValidators
                .Select(v => v.Provider.ToString().ToLower())
        };

        return Ok(config);
    }
}

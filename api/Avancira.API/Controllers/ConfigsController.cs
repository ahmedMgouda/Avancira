using Avancira.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using Avancira.Application.Auth;

namespace Avancira.API.Controllers;

[AllowAnonymous]
[Route("api/configs")]
public class ConfigsController : BaseApiController
{
    private readonly StripeOptions _stripeOptions;
    private readonly PayPalOptions _payPalOptions;
    private readonly GoogleOptions _googleOptions;
    private readonly FacebookOptions _facebookOptions;

    public ConfigsController(
        IOptions<StripeOptions> stripeOptions,
        IOptions<PayPalOptions> payPalOptions,
        IOptions<GoogleOptions> googleOptions,
        IOptions<FacebookOptions> facebookOptions
    )
    {
        _stripeOptions = stripeOptions.Value;
        _payPalOptions = payPalOptions.Value;
        _googleOptions = googleOptions.Value;
        _facebookOptions = facebookOptions.Value;
    }

    // Read
    [HttpGet]
    public IActionResult GetConfig()
    {
        var providers = new List<SocialProvider>();
        if (!string.IsNullOrWhiteSpace(_googleOptions.ClientId))
            providers.Add(SocialProvider.Google);
        if (!string.IsNullOrWhiteSpace(_facebookOptions.AppId))
            providers.Add(SocialProvider.Facebook);

        var config = new
        {
            stripePublishableKey = _stripeOptions.PublishableKey,
            payPalClientId = _payPalOptions.ClientId,
            googleMapsApiKey = _googleOptions.ApiKey,
            googleClientId = _googleOptions.ClientId,
            facebookAppId = _facebookOptions.AppId,
            enabledSocialProviders = providers
                .Select(p => p.ToString().ToLowerInvariant())
        };

        return Ok(config);
    }
}

using Avancira.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using Avancira.Application.Auth;
using Avancira.Application.Config;

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

        var config = new Dictionary<ConfigKey, string>
        {
            [ConfigKey.StripePublishableKey] = _stripeOptions.PublishableKey,
            [ConfigKey.PayPalClientId] = _payPalOptions.ClientId,
            [ConfigKey.GoogleMapsApiKey] = _googleOptions.ApiKey,
            [ConfigKey.GoogleClientId] = _googleOptions.ClientId,
            [ConfigKey.FacebookAppId] = _facebookOptions.AppId
        };

        return Ok(new { config, enabledSocialProviders = providers });
    }
}

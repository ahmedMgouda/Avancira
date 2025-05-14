using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Backend.Controllers;

[AllowAnonymous]
[Route("api/configs")]
[ApiController]
public class ConfigsAPIController : BaseController
{
    private readonly StripeOptions _stripeOptions;
    private readonly PayPalOptions _payPalOptions;
    private readonly GoogleOptions _googleOptions;
    private readonly FacebookOptions _facebookOptions;

    public ConfigsAPIController(
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
        var config = new
        {
            stripePublishableKey = _stripeOptions.PublishableKey,
            payPalClientId = _payPalOptions.ClientId,
            googleMapsApiKey = _googleOptions.ApiKey,
            googleClientId = _googleOptions.ClientId,
            googleClientSecret = _googleOptions.ClientSecret,
            facebookAppId = _facebookOptions.AppId
        };

        return Ok(config);
    }
}



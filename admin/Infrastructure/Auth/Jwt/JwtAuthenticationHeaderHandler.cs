using System.Text.Json;
using Avancira.Admin.Infrastructure.Storage;
using Avancira.Shared.Authorization;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using System.Net.Http.Headers;

namespace Avancira.Admin.Infrastructure.Auth.Jwt;

public class JwtAuthenticationHeaderHandler : DelegatingHandler
{
    private readonly IAccessTokenProviderAccessor _tokenProviderAccessor;
    private readonly NavigationManager _navigation;
    private readonly ILocalStorageService _localStorage;

    public JwtAuthenticationHeaderHandler(
        IAccessTokenProviderAccessor tokenProviderAccessor,
        NavigationManager navigation,
        ILocalStorageService localStorage)
    {
        _tokenProviderAccessor = tokenProviderAccessor;
        _navigation = navigation;
        _localStorage = localStorage;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? deviceId = await _localStorage.GetItemAsync<string>(StorageConstants.Local.DeviceId);

        if (request.RequestUri?.AbsolutePath.Contains("/auth") != true)
        {
            if (await _tokenProviderAccessor.TokenProvider.GetAccessTokenAsync() is string token)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                try
                {
                    string payload = token.Split('.')[1];
                    byte[] jsonBytes = ParseBase64WithoutPadding(payload);
                    var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);
                    if (keyValuePairs != null &&
                        keyValuePairs.TryGetValue(AvanciraClaims.DeviceId, out var claim) &&
                        claim is not null)
                    {
                        deviceId = claim.ToString();
                        await _localStorage.SetItemAsync(StorageConstants.Local.DeviceId, deviceId);
                    }
                }
                catch
                {
                    // ignore decoding errors
                }
            }
            else
            {
                _navigation.NavigateTo("/login");
            }
        }

        if (string.IsNullOrEmpty(deviceId))
        {
            deviceId = Guid.NewGuid().ToString();
            await _localStorage.SetItemAsync(StorageConstants.Local.DeviceId, deviceId);
        }

        request.Headers.TryAddWithoutValidation("Device-Id", deviceId);

        return await base.SendAsync(request, cancellationToken);
    }

    private static byte[] ParseBase64WithoutPadding(string payload)
    {
        payload = payload.Trim().Replace('-', '+').Replace('_', '/');
        string base64 = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        return Convert.FromBase64String(base64);
    }
}


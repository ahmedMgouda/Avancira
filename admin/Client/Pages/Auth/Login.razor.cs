using Avancira.Admin.Client.Components;
using Avancira.Admin.Infrastructure.Api;
using Avancira.Shared.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace Avancira.Admin.Client.Pages.Auth;

public partial class Login()
{
    [CascadingParameter]
    public Task<AuthenticationState> AuthState { get; set; } = default!;

    private FshValidation? _customValidation;

    public bool BusySubmitting { get; set; }

    private readonly TokenGenerationDto _tokenRequest = new();
    private bool _passwordVisibility;
    private InputType _passwordInput = InputType.Password;
    private string _passwordInputIcon = Icons.Material.Filled.VisibilityOff;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthState;
        if (authState.User.Identity?.IsAuthenticated is true)
        {
            Navigation.NavigateTo("/");
        }
    }

    private void TogglePasswordVisibility()
    {
        if (_passwordVisibility)
        {
            _passwordVisibility = false;
            _passwordInputIcon = Icons.Material.Filled.VisibilityOff;
            _passwordInput = InputType.Password;
        }
        else
        {
            _passwordVisibility = true;
            _passwordInputIcon = Icons.Material.Filled.Visibility;
            _passwordInput = InputType.Text;
        }
    }

    private void FillAdministratorCredentials()
    {
        _tokenRequest.Email = AppConstants.EmailAddress;
        _tokenRequest.Password = AppConstants.DefaultPassword;
    }

    private async Task SubmitAsync()
    {
        BusySubmitting = true;

        if (await ApiHelper.ExecuteCallGuardedAsync(
            () => authService.LoginAsync(_tokenRequest),
            Toast,
            _customValidation))
        {
            Toast.Add($"Logged in as {_tokenRequest.Email}", Severity.Info);
        }

        BusySubmitting = false;
    }
}

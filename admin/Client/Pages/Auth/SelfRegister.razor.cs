using Avancira.Admin.Client.Components;
using Avancira.Admin.Infrastructure.Api;
using Avancira.Shared.Authorization;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Avancira.Admin.Client.Pages.Auth;

public partial class SelfRegister
{
    private readonly RegisterUserDto _createUserRequest = new();
    private FshValidation? _customValidation;
    private bool BusySubmitting { get; set; }

    [Inject]
    private IApiClient UsersClient { get; set; } = default!;


    private bool _passwordVisibility;
    private InputType _passwordInput = InputType.Password;
    private string _passwordInputIcon = Icons.Material.Filled.VisibilityOff;

    private async Task SubmitAsync()
    {
        BusySubmitting = true;

        var response = await ApiHelper.ExecuteCallGuardedAsync(
            () => UsersClient.SelfRegisterUserAsync(_createUserRequest),
            Toast, Navigation,
            _customValidation);

        if (response != null)
        {
            Toast.Add($"user {response.UserId} registered.", Severity.Success);
            Navigation.NavigateTo("/login");
        }

        BusySubmitting = false;
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
}

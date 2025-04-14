using Avancira.Admin.Client.Components;
using Avancira.Admin.Infrastructure.Api;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Avancira.Admin.Client.Pages.Identity.Account;

public partial class Security
{
    [Inject]
    public IApiClient PersonalClient { get; set; } = default!;

    private readonly ChangePasswordDto _passwordModel = new();

    private FshValidation? _customValidation;

    private async Task ChangePasswordAsync()
    {
        if (await ApiHelper.ExecuteCallGuardedAsync(
            () => PersonalClient.ChangePasswordAsync(_passwordModel),
            Toast,
            _customValidation,
            "Password Changed!"))
        {
            _passwordModel.Password = string.Empty;
            _passwordModel.NewPassword = string.Empty;
            _passwordModel.ConfirmNewPassword = string.Empty;
        }
    }

    private bool _currentPasswordVisibility;
    private InputType _currentPasswordInput = InputType.Password;
    private string _currentPasswordInputIcon = Icons.Material.Filled.VisibilityOff;
    private bool _newPasswordVisibility;
    private InputType _newPasswordInput = InputType.Password;
    private string _newPasswordInputIcon = Icons.Material.Filled.VisibilityOff;

    private void TogglePasswordVisibility(bool newPassword)
    {
        if (newPassword)
        {
            if (_newPasswordVisibility)
            {
                _newPasswordVisibility = false;
                _newPasswordInputIcon = Icons.Material.Filled.VisibilityOff;
                _newPasswordInput = InputType.Password;
            }
            else
            {
                _newPasswordVisibility = true;
                _newPasswordInputIcon = Icons.Material.Filled.Visibility;
                _newPasswordInput = InputType.Text;
            }
        }
        else
        {
            if (_currentPasswordVisibility)
            {
                _currentPasswordVisibility = false;
                _currentPasswordInputIcon = Icons.Material.Filled.VisibilityOff;
                _currentPasswordInput = InputType.Password;
            }
            else
            {
                _currentPasswordVisibility = true;
                _currentPasswordInputIcon = Icons.Material.Filled.Visibility;
                _currentPasswordInput = InputType.Text;
            }
        }
    }
}

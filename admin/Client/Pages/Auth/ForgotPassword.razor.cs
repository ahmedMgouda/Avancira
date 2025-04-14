using Avancira.Admin.Client.Components;
using Avancira.Admin.Infrastructure.Api;
using Avancira.Shared.Authorization;
using Microsoft.AspNetCore.Components;

namespace Avancira.Admin.Client.Pages.Auth;

public partial class ForgotPassword
{
    private readonly ForgotPasswordDto _forgotPasswordRequest = new();
    private FshValidation? _customValidation;
    private bool BusySubmitting { get; set; }

    [Inject]
    private IApiClient UsersClient { get; set; } = default!;

    private async Task SubmitAsync()
    {
        BusySubmitting = true;

        await ApiHelper.ExecuteCallGuardedAsync(
            () => UsersClient.ForgotPasswordAsync(_forgotPasswordRequest),
            Toast,
            _customValidation);

        BusySubmitting = false;
    }
}

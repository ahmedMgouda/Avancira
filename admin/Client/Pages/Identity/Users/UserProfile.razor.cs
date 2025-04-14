using Avancira.Admin.Client.Components;
using Avancira.Admin.Infrastructure.Api;
using Avancira.Admin.Infrastructure.Auth;
using Avancira.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Avancira.Admin.Client.Pages.Identity.Users;

public partial class UserProfile
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthorizationService AuthService { get; set; } = default!;
    [Inject]
    protected IApiClient UsersClient { get; set; } = default!;

    [Parameter]
    public string? Id { get; set; }
    [Parameter]
    public string? Title { get; set; }
    [Parameter]
    public string? Description { get; set; }

    private bool _active;
    private bool _emailConfirmed;
    private char _firstLetterOfName;
    private string? _firstName;
    private string? _lastName;
    private string? _phoneNumber;
    private string? _email;
    private Uri? _imageUrl;
    private bool _loaded;
    private bool _canToggleUserStatus;

    private async Task ToggleUserStatus()
    {
        var request = new ToggleUserStatusDto { ActivateUser = _active, UserId = Id };
        await ApiHelper.ExecuteCallGuardedAsync(() => UsersClient.ToggleUserStatusAsync(Id!, request), Toast);
        Navigation.NavigateTo("/identity/users");
    }

    [Parameter]
    public string? ImageUrl { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (await ApiHelper.ExecuteCallGuardedAsync(
                () => UsersClient.GetUserByIdAsync(Id!), Toast, Navigation)
            is UserDetailDto user)
        {
            _firstName = user.FirstName;
            _lastName = user.LastName;
            _email = user.Email;
            _phoneNumber = user.PhoneNumber;
            _active = user.IsActive;
            _emailConfirmed = user.EmailConfirmed;
            _imageUrl = user.ImageUrl;
            Title = $"{_firstName} {_lastName}'s Profile";
            Description = _email;
            if (_firstName?.Length > 0)
            {
                _firstLetterOfName = _firstName.ToUpperInvariant().FirstOrDefault();
            }
        }

        var state = await AuthState;
        _canToggleUserStatus = await AuthService.HasPermissionAsync(state.User, AvanciraActions.Update, AvanciraResources.Users);
        _loaded = true;
    }
}

﻿using Avancira.Admin.Client.Components;
using Avancira.Admin.Infrastructure.Api;
using Avancira.Admin.Infrastructure.Auth;
using Avancira.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Avancira.Admin.Client.Pages.Identity.Users;

public partial class UserRoles
{
    [Parameter]
    public string? Id { get; set; }
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthorizationService AuthService { get; set; } = default!;
    [Inject]
    protected IApiClient UsersClient { get; set; } = default!;

    private List<UserRoleDetailDto> _userRolesList = default!;

    private string _title = string.Empty;
    private string _description = string.Empty;

    private string _searchString = string.Empty;

    private bool _canEditUsers;
    private bool _canSearchRoles;
    private bool _loaded;

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthState;

        _canEditUsers = await AuthService.HasPermissionAsync(state.User, AvanciraActions.Update, AvanciraResources.Users);
        _canSearchRoles = await AuthService.HasPermissionAsync(state.User, AvanciraActions.View, AvanciraResources.UserRoles);

        if (await ApiHelper.ExecuteCallGuardedAsync(
                () => UsersClient.GetUserByIdAsync(Id!), Toast, Navigation)
            is UserDetailDto user)
        {
            _title = $"{user.FirstName} {user.LastName}'s Roles";
            _description = string.Format("Manage {0} {1}'s Roles", user.FirstName, user.LastName);

            if (await ApiHelper.ExecuteCallGuardedAsync(
                    () => UsersClient.GetUserRolesAsync(user.Id), Toast, Navigation)
                is ICollection<UserRoleDetailDto> response)
            {
                _userRolesList = response.ToList();
            }
        }

        _loaded = true;
    }

    private async Task SaveAsync()
    {
        var request = new AssignUserRoleDto()
        {
            UserRoles = _userRolesList
        };

        Console.WriteLine($"roles : {request.UserRoles.Count}");

        await ApiHelper.ExecuteCallGuardedAsync(
                () => UsersClient.AssignRolesToUserAsync(Id, request),
                Toast,
                successMessage: "updated user roles");

        Navigation.NavigateTo("/identity/users");
    }

    private bool Search(UserRoleDetailDto userRole) =>
        string.IsNullOrWhiteSpace(_searchString)
            || userRole.RoleName?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) is true;
}

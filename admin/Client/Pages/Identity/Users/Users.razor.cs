﻿using Avancira.Admin.Client.Components.EntityTable;
using Avancira.Admin.Infrastructure.Api;
using Avancira.Admin.Infrastructure.Auth;
using Avancira.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace Avancira.Admin.Client.Pages.Identity.Users;

public partial class Users
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthorizationService AuthService { get; set; } = default!;

    [Inject]
    protected IApiClient UsersClient { get; set; } = default!;

    protected EntityClientTableContext<UserDetailDto, Guid, RegisterUserDto> Context { get; set; } = default!;

    private bool _canExportUsers;
    private bool _canViewAuditTrails;
    private bool _canViewRoles;

    // Fields for editform
    protected string Password { get; set; } = string.Empty;
    protected string ConfirmPassword { get; set; } = string.Empty;

    private bool _passwordVisibility;
    private InputType _passwordInput = InputType.Password;
    private string _passwordInputIcon = Icons.Material.Filled.VisibilityOff;

    protected override async Task OnInitializedAsync()
    {
        var user = (await AuthState).User;
        _canExportUsers = await AuthService.HasPermissionAsync(user, AvanciraActions.Export, AvanciraResources.Users);
        _canViewRoles = await AuthService.HasPermissionAsync(user, AvanciraActions.View, AvanciraResources.UserRoles);
        _canViewAuditTrails = await AuthService.HasPermissionAsync(user, AvanciraActions.View, AvanciraResources.AuditTrails);

        Context = new(
            entityName: "User",
            entityNamePlural: "Users",
            entityResource: AvanciraResources.Users,
            searchAction: AvanciraActions.View,
            updateAction: string.Empty,
            deleteAction: string.Empty,
            fields: new()
            {
                new(user => user.FirstName,"First Name"),
                new(user => user.LastName, "Last Name"),
                new(user => user.UserName, "UserName"),
                new(user => user.Email, "Email"),
                new(user => user.PhoneNumber, "PhoneNumber"),
                new(user => user.EmailConfirmed, "Email Confirmation", Type: typeof(bool)),
                new(user => user.IsActive, "Active", Type: typeof(bool))
            },
            idFunc: user => user.Id,
            loadDataFunc: async () => (await UsersClient.GetUsersListAsync()).ToList(),
            searchFunc: (searchString, user) =>
                string.IsNullOrWhiteSpace(searchString)
                    || user.FirstName?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true
                    || user.LastName?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true
                    || user.Email?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true
                    || user.PhoneNumber?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true
                    || user.UserName?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true,
            createFunc: user => UsersClient.RegisterUserAsync(user),
            hasExtraActionsFunc: () => true,
            exportAction: string.Empty);
    }

    private void ViewProfile(in Guid userId) =>
        Navigation.NavigateTo($"/identity/users/{userId}/profile");

    private void ManageRoles(in Guid userId) =>
        Navigation.NavigateTo($"/identity/users/{userId}/roles");
    private void ViewAuditTrails(in Guid userId) =>
        Navigation.NavigateTo($"/identity/users/{userId}/audit-trail");

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

        Context.AddEditModal.ForceRender();
    }
}

﻿using Avancira.Admin.Infrastructure.Auth;
using Avancira.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Avancira.Admin.Client.Layout;

public partial class NavMenu
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthorizationService AuthService { get; set; } = default!;

    private bool _canViewHangfire;
    private bool _canViewDashboard;
    private bool _canViewRoles;
    private bool _canViewUsers;
    private bool _canViewProducts;
    private bool _canViewBrands;
    private bool _canViewTodos;
    private bool _canViewTenants;
    private bool _canViewAuditTrails;
    private bool CanViewAdministrationGroup => _canViewUsers || _canViewRoles || _canViewTenants;

    protected override async Task OnParametersSetAsync()
    {
        var user = (await AuthState).User;
        _canViewHangfire = await AuthService.HasPermissionAsync(user, AvanciraActions.View, AvanciraResources.Hangfire);
        _canViewDashboard = await AuthService.HasPermissionAsync(user, AvanciraActions.View, AvanciraResources.Dashboard);
        _canViewRoles = await AuthService.HasPermissionAsync(user, AvanciraActions.View, AvanciraResources.Roles);
        _canViewUsers = await AuthService.HasPermissionAsync(user, AvanciraActions.View, AvanciraResources.Users);
        _canViewProducts = await AuthService.HasPermissionAsync(user, AvanciraActions.View, AvanciraResources.Products);
        _canViewBrands = await AuthService.HasPermissionAsync(user, AvanciraActions.View, AvanciraResources.Brands);
        _canViewTodos = await AuthService.HasPermissionAsync(user, AvanciraActions.View, AvanciraResources.Todos);
        _canViewTenants = await AuthService.HasPermissionAsync(user, AvanciraActions.View, AvanciraResources.Tenants);
        _canViewAuditTrails = await AuthService.HasPermissionAsync(user, AvanciraActions.View, AvanciraResources.AuditTrails);
    }
}

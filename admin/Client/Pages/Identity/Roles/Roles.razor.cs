using Avancira.Admin.Client.Components.EntityTable;
using Avancira.Admin.Infrastructure.Api;
using Avancira.Admin.Infrastructure.Auth;
using Avancira.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Avancira.Admin.Client.Pages.Identity.Roles;

public partial class Roles
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthorizationService AuthService { get; set; } = default!;
    [Inject]
    private IApiClient RolesClient { get; set; } = default!;

    protected EntityClientTableContext<RoleDto, string?, CreateOrUpdateRoleDto> Context { get; set; } = default!;

    private bool _canViewRoleClaims;

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthState;
        _canViewRoleClaims = await AuthService.HasPermissionAsync(state.User, AvanciraActions.View, AvanciraResources.RoleClaims);

        Context = new(
            entityName: "Role",
            entityNamePlural: "Roles",
            entityResource: AvanciraResources.Roles,
            searchAction: AvanciraActions.View,
            fields: new()
            {
                new(role => role.Id, "Id"),
                new(role => role.Name,"Name"),
                new(role => role.Description, "Description")
            },
            idFunc: role => role.Id,
            loadDataFunc: async () => (await RolesClient.GetRolesAsync()).ToList(),
            searchFunc: (searchString, role) =>
                string.IsNullOrWhiteSpace(searchString)
                    || role.Name?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true
                    || role.Description?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true,
            createFunc: async role => await RolesClient.CreateOrUpdateRoleAsync(role),
            updateFunc: async (_, role) => await RolesClient.CreateOrUpdateRoleAsync(role),
            deleteFunc: async id => await RolesClient.DeleteRoleAsync(id!),
            hasExtraActionsFunc: () => _canViewRoleClaims,
            canUpdateEntityFunc: e => !AvanciraRoles.IsDefault(e.Name!),
            canDeleteEntityFunc: e => !AvanciraRoles.IsDefault(e.Name!),
            exportAction: string.Empty);
    }

    private void ManagePermissions(string? roleId)
    {
        ArgumentNullException.ThrowIfNull(roleId, nameof(roleId));
        Navigation.NavigateTo($"/identity/roles/{roleId}/permissions");
    }
}

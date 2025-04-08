using System.Collections.ObjectModel;

namespace Avancira.Shared.Authorization;
    public static class AvanciraPermissions
    {
     private static readonly AvanciraPermission[] AllPermissions =
    [     
        //identity
        new("View Users", AvanciraActions.View, AvanciraResources.Users),
        new("Search Users", AvanciraActions.Search, AvanciraResources.Users),
        new("Create Users", AvanciraActions.Create, AvanciraResources.Users),
        new("Update Users", AvanciraActions.Update, AvanciraResources.Users),
        new("Delete Users", AvanciraActions.Delete, AvanciraResources.Users),
        new("Export Users", AvanciraActions.Export, AvanciraResources.Users),
        new("View UserRoles", AvanciraActions.View, AvanciraResources.UserRoles),
        new("Update UserRoles", AvanciraActions.Update, AvanciraResources.UserRoles),
        new("View Roles", AvanciraActions.View, AvanciraResources.Roles),
        new("Create Roles", AvanciraActions.Create, AvanciraResources.Roles),
        new("Update Roles", AvanciraActions.Update, AvanciraResources.Roles),
        new("Delete Roles", AvanciraActions.Delete, AvanciraResources.Roles),
        new("View RoleClaims", AvanciraActions.View, AvanciraResources.RoleClaims),
        new("Update RoleClaims", AvanciraActions.Update, AvanciraResources.RoleClaims),
        
        //products
        new("View Products", AvanciraActions.View, AvanciraResources.Products, IsBasic: true),
        new("Search Products", AvanciraActions.Search, AvanciraResources.Products, IsBasic: true),
        new("Create Products", AvanciraActions.Create, AvanciraResources.Products),
        new("Update Products", AvanciraActions.Update, AvanciraResources.Products),
        new("Delete Products", AvanciraActions.Delete, AvanciraResources.Products),
        new("Export Products", AvanciraActions.Export, AvanciraResources.Products),

        //brands
        new("View Brands", AvanciraActions.View, AvanciraResources.Brands, IsBasic: true),
        new("Search Brands", AvanciraActions.Search, AvanciraResources.Brands, IsBasic: true),
        new("Create Brands", AvanciraActions.Create, AvanciraResources.Brands),
        new("Update Brands", AvanciraActions.Update, AvanciraResources.Brands),
        new("Delete Brands", AvanciraActions.Delete, AvanciraResources.Brands),
        new("Export Brands", AvanciraActions.Export, AvanciraResources.Brands),

        //todos
        new("View Todos", AvanciraActions.View, AvanciraResources.Todos, IsBasic: true),
        new("Search Todos", AvanciraActions.Search, AvanciraResources.Todos, IsBasic: true),
        new("Create Todos", AvanciraActions.Create, AvanciraResources.Todos),
        new("Update Todos", AvanciraActions.Update, AvanciraResources.Todos),
        new("Delete Todos", AvanciraActions.Delete, AvanciraResources.Todos),
        new("Export Todos", AvanciraActions.Export, AvanciraResources.Todos),

         new("View Hangfire", AvanciraActions.View, AvanciraResources.Hangfire),
         new("View Dashboard", AvanciraActions.View, AvanciraResources.Dashboard),

        //audit
        new("View Audit Trails", AvanciraActions.View, AvanciraResources.AuditTrails),
    ];

    public static IReadOnlyList<AvanciraPermission> All { get; } = new ReadOnlyCollection<AvanciraPermission>(AllPermissions);
    public static IReadOnlyList<AvanciraPermission> Root { get; } = new ReadOnlyCollection<AvanciraPermission>(AllPermissions.Where(p => p.IsRoot).ToArray());
    public static IReadOnlyList<AvanciraPermission> Admin { get; } = new ReadOnlyCollection<AvanciraPermission>(AllPermissions.Where(p => !p.IsRoot).ToArray());
    public static IReadOnlyList<AvanciraPermission> Basic { get; } = new ReadOnlyCollection<AvanciraPermission>(AllPermissions.Where(p => p.IsBasic).ToArray());
}
public record AvanciraPermission(string Description, string Action, string Resource, bool IsBasic = false, bool IsRoot = false)
{
    public string Name => NameFor(Action, Resource);
    public static string NameFor(string action, string resource)
    {
        return $"Permissions.{resource}.{action}";
    }
}
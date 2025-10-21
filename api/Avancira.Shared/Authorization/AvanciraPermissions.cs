using System.Collections.ObjectModel;

namespace Avancira.Shared.Authorization;

public static class AvanciraPermissions
{
    private static readonly AvanciraPermission[] AllPermissions =
    [
        // ──────────────────────────────
        // USER MANAGEMENT (Admin)
        // ──────────────────────────────
        new("View Users", AvanciraActions.View, AvanciraResources.Users),
        new("Search Users", AvanciraActions.Search, AvanciraResources.Users),
        new("Create Users", AvanciraActions.Create, AvanciraResources.Users),
        new("Update Users", AvanciraActions.Update, AvanciraResources.Users),
        new("Delete Users", AvanciraActions.Delete, AvanciraResources.Users),
        new("Export Users", AvanciraActions.Export, AvanciraResources.Users),

        // Roles & Role Claims
        new("View Roles", AvanciraActions.View, AvanciraResources.Roles),
        new("Create Roles", AvanciraActions.Create, AvanciraResources.Roles),
        new("Update Roles", AvanciraActions.Update, AvanciraResources.Roles),
        new("Delete Roles", AvanciraActions.Delete, AvanciraResources.Roles),
        new("View RoleClaims", AvanciraActions.View, AvanciraResources.RoleClaims),
        new("Update RoleClaims", AvanciraActions.Update, AvanciraResources.RoleClaims),

        // ──────────────────────────────
        // LISTINGS & CATEGORIES
        // ──────────────────────────────
        // Student permissions (basic)
        new("View Listings", AvanciraActions.View, AvanciraResources.Listings, IsStudent: true),
        new("Search Listings", AvanciraActions.Search, AvanciraResources.Listings, IsStudent: true),
        new("View Categories", AvanciraActions.View, AvanciraResources.Categories, IsStudent: true),
        new("Search Categories", AvanciraActions.Search, AvanciraResources.Categories, IsStudent: true),

        // Tutor permissions
        new("Create Listings", AvanciraActions.Create, AvanciraResources.Listings, IsTutor: true),
        new("Update Listings", AvanciraActions.Update, AvanciraResources.Listings, IsTutor: true),
        new("Delete Listings", AvanciraActions.Delete, AvanciraResources.Listings, IsTutor: true),
        new("View My Lessons", AvanciraActions.View, AvanciraResources.Lessons, IsTutor: true),
        new("Manage My Schedule", AvanciraActions.Update, AvanciraResources.Schedules, IsTutor: true),

        // Admin (full)
        new("Manage Listings", AvanciraActions.Update, AvanciraResources.Listings),
        new("Delete Listings", AvanciraActions.Delete, AvanciraResources.Listings),
        new("Export Listings", AvanciraActions.Export, AvanciraResources.Listings),

        // ──────────────────────────────
        // DASHBOARD & SYSTEM
        // ──────────────────────────────
        new("View Dashboard", AvanciraActions.View, AvanciraResources.Dashboard),
        new("View Hangfire", AvanciraActions.View, AvanciraResources.Hangfire),
        new("View Audit Trails", AvanciraActions.View, AvanciraResources.AuditTrails)
    ];

    // ──────────────────────────────
    // Permission group accessors
    // ──────────────────────────────
    public static IReadOnlyList<AvanciraPermission> All { get; } = new ReadOnlyCollection<AvanciraPermission>(AllPermissions);
    public static IReadOnlyList<AvanciraPermission> Admin { get; } = new ReadOnlyCollection<AvanciraPermission>(AllPermissions.Where(p => !p.IsTutor && !p.IsStudent).ToArray());
    public static IReadOnlyList<AvanciraPermission> Tutor { get; } = new ReadOnlyCollection<AvanciraPermission>(AllPermissions.Where(p => p.IsTutor).ToArray());
    public static IReadOnlyList<AvanciraPermission> Student { get; } = new ReadOnlyCollection<AvanciraPermission>(AllPermissions.Where(p => p.IsStudent).ToArray());
}

public record AvanciraPermission(
    string Description,
    string Action,
    string Resource,
    bool IsTutor = false,
    bool IsStudent = false,
    bool IsRoot = false)
{
    public string Name => NameFor(Action, Resource);

    public static string NameFor(string action, string resource)
        => $"Permissions.{resource}.{action}";
}

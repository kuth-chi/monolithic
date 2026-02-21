namespace Monolithic.Api.Modules.Identity.Domain;

/// <summary>
/// Centralised constants for roles that are seeded by the system and may never be deleted.
/// Every name here must exactly match the <c>Name</c> column in <c>AspNetRoles</c>.
/// </summary>
public static class SystemRoleNames
{
    /// <summary>Platform superuser — owns everything, granted <c>*:full</c>.</summary>
    public const string Owner = "Owner";

    /// <summary>System-level administrator — full platform management access.</summary>
    public const string SystemAdmin = "System Admin";

    /// <summary>The complete set of role names that are immutable / cannot be deleted or renamed.</summary>
    public static readonly IReadOnlySet<string> Protected = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Owner,
        SystemAdmin,
    };

    /// <summary>Returns <c>true</c> when <paramref name="roleName"/> is a protected system role.</summary>
    public static bool IsProtected(string? roleName) =>
        roleName is not null && Protected.Contains(roleName);
}

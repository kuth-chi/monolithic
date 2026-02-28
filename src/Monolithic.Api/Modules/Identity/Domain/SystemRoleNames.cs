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

    /// <summary>Staff member — operational access to most modules.</summary>
    public const string Staff = "Staff";

    /// <summary>Regular user — read-only baseline access.</summary>
    public const string User = "User";

    /// <summary>
    /// The complete set of role names that are immutable / cannot be deleted or renamed.
    /// All four roles are seeded by the system on first run.
    /// </summary>
    public static readonly IReadOnlySet<string> Protected = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Owner,
        SystemAdmin,
        Staff,
        User,
    };

    /// <summary>Returns <c>true</c> when <paramref name="roleName"/> is a protected system role.</summary>
    public static bool IsProtected(string? roleName) =>
        roleName is not null && Protected.Contains(roleName);
}

namespace Monolithic.Api.Modules.Platform.Core.Abstractions;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Describes a single permission (capability) that a module declares.
///
/// Modules call <see cref="IModule.GetPermissions"/> to self-register their
/// permission catalog. The <c>ModuleRegistry</c> aggregates all permissions
/// and the <c>IAuthorizationPolicyRegistry</c> seeds them as named
/// <c>Authorization.Policy</c> entries at startup — no manual policy
/// registration needed when adding new modules (OWASP A01 compliance).
///
/// Convention for permission keys:
///   "{module}:{resource}:{action}"
///   e.g. "inventory:items:read", "finance:journal-entries:write"
///
/// Actions:  read | write | delete | approve | export | admin
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public sealed record PermissionDescriptor(
    /// <summary>
    /// Stable lowercase dot/colon-separated permission key.
    /// Convention: "{moduleId}:{resource}:{action}".
    /// </summary>
    string Permission,

    /// <summary>Human-readable name displayed in the role editor UI.</summary>
    string DisplayName,

    /// <summary>Module that owns this permission.</summary>
    string ModuleId,

    /// <summary>Markdown description shown in the permission details panel.</summary>
    string? Description = null,

    /// <summary>
    /// Built-in roles that receive this permission by default on first seed.
    /// e.g. ["admin", "manager"].  End-users can modify grants via the Roles UI.
    /// </summary>
    string[]? DefaultRoles = null,

    /// <summary>
    /// When true, this is a sensitive/destructive permission and should be
    /// highlighted in the role editor (e.g. delete, approve, admin actions).
    /// </summary>
    bool IsSensitive = false,

    /// <summary>
    /// When <c>true</c> this permission is a <b>self-data</b> permission using
    /// the action token <c>self</c> (e.g. <c>users:profiles:self</c>).
    ///
    /// Self-data permissions are evaluated via
    /// <c>SelfOwnershipAuthorizationHandler</c>: the holder may only access
    /// resources they <em>own</em> (OwnerId == callerId).
    /// They are seeded with <see cref="Permission.IsSelfScoped"/> = true in
    /// the database so the role-editor UI can display a visual distinction.
    /// </summary>
    bool IsSelfData = false
);

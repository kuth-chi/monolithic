namespace Monolithic.Api.Modules.Platform.Core.Abstractions;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Describes a single navigation entry that a module contributes to the
/// Admin or Operation UI shell sidebar.
///
/// The frontend calls <c>GET /api/v1/platform/navigation?context=admin</c>
/// (or <c>operation</c>) and builds its sidebar from the returned tree —
/// no frontend hardcoding required (true plug-and-play).
///
/// Usage (in a module's <see cref="IModule.GetNavigationItems"/>):
/// <code>
/// yield return new NavigationItem(
///     Key:      "inventory.items",
///     Label:    "Items",
///     Route:    "/inventory/items",
///     Context:  UiContext.Operation,
///     ModuleId: ModuleId,
///     Icon:     "cube",
///     Order:    10,
///     RequiredPermissions: ["inventory:items:read"]);
/// </code>
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public sealed record NavigationItem(
    /// <summary>
    /// Stable, globally-unique key. Convention: "{moduleId}.{feature}".
    /// Used by the frontend to track active state and by tests to assert menus.
    /// </summary>
    string Key,

    /// <summary>Translated display label. Override per locale on the frontend.</summary>
    string Label,

    /// <summary>
    /// Absolute frontend route, e.g. "/inventory/items", "/admin/users".
    /// Must start with "/" and match the Next.js App Router file structure.
    /// </summary>
    string Route,

    /// <summary>Which UI shell this item belongs to.</summary>
    UiContext Context,

    /// <summary>Module that owns this navigation item.</summary>
    string ModuleId,

    /// <summary>
    /// Heroicons v2 slug (outline), e.g. "cube", "chart-bar", "users".
    /// Null = no icon rendered.
    /// </summary>
    string? Icon = null,

    /// <summary>
    /// Key of the parent navigation item; null for top-level entries.
    /// Enables nested/grouped menus without a separate Group concept.
    /// </summary>
    string? ParentKey = null,

    /// <summary>Sort order within the same parent group (ascending, 0 = first).</summary>
    int Order = 0,

    /// <summary>
    /// Permission keys the current user must hold to see this item.
    /// Empty = visible to any authenticated user.
    /// OWASP A01: the API also enforces permissions at the controller level;
    /// hiding items in the nav is a UX aid only, NOT a security boundary.
    /// </summary>
    string[]? RequiredPermissions = null,

    /// <summary>
    /// When true, this item renders as a section header / group label rather
    /// than a clickable link (Route is ignored).
    /// </summary>
    bool IsGroup = false,

    /// <summary>
    /// Badge text displayed next to the label (e.g. "NEW", "BETA").
    /// Null = no badge.
    /// </summary>
    string? Badge = null
);

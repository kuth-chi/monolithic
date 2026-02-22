using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.Platform.Core.Abstractions;
using Monolithic.Api.Modules.Platform.Core.Infrastructure;

namespace Monolithic.Api.Controllers.V1;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Platform manifest — the OS kernel's public API consumed by the frontend
/// to dynamically build its Admin UI and Operation UI shells.
///
/// Endpoints:
///   GET  /api/v1/platform/modules             – registered module catalog
///   GET  /api/v1/platform/widgets             – dashboard widget catalog
///   GET  /api/v1/platform/permissions         – RBAC permission catalog
///   GET  /api/v1/platform/navigation/admin    – Admin UI sidebar tree
///   GET  /api/v1/platform/navigation/operation– Operation UI sidebar tree
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/v1/platform")]
public sealed class PlatformInfoController(ModuleRegistry registry) : ControllerBase
{
    // ── Module Catalog ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all registered modules with their identity, version, dependencies,
    /// icon, and description.
    /// </summary>
    [HttpGet("modules")]
    [RequirePermission("platform:info:read")]
    public IActionResult GetModules()
        => Ok(registry.Modules.Select(m => new
        {
            m.ModuleId,
            m.DisplayName,
            m.Version,
            m.Description,
            m.Icon,
            Dependencies = m.Dependencies.ToArray(),
            PermissionCount   = m.GetPermissions().Count(),
            NavigationCount   = m.GetNavigationItems().Count(),
            WidgetCount       = m.GetWidgets().Count(),
            TemplateCount     = m.GetDefaultTemplates().Count(),
        }));

    // ── Widget Catalog ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all dashboard widgets across all modules.
    /// Used by the user-preference widget-picker to populate the catalog.
    /// </summary>
    [HttpGet("widgets")]
    [RequirePermission("platform:info:read")]
    public IActionResult GetWidgets()
        => Ok(registry.GetAllWidgets());

    // ── Permission Catalog (OWASP A01) ────────────────────────────────────────

    /// <summary>
    /// Returns the full RBAC permission catalog — all permissions declared by
    /// all modules. Used by the Roles / Access Control admin UI to build the
    /// permission assignment grid.
    /// </summary>
    [HttpGet("permissions")]
    [RequirePermission("users:roles:read")]
    public IActionResult GetPermissions()
        => Ok(registry.GetAllPermissions()
            .Select(p => new
            {
                p.Permission,
                p.DisplayName,
                p.ModuleId,
                p.Description,
                p.DefaultRoles,
                p.IsSensitive,
            })
            .OrderBy(p => p.ModuleId)
            .ThenBy(p => p.Permission));

    // ── Navigation Manifest (Admin UI / Operation UI) ─────────────────────────

    /// <summary>
    /// Returns the complete navigation tree for the <b>Admin UI</b> shell.
    ///
    /// The frontend calls this once on mount and builds its sidebar dynamically.
    /// Items marked <see cref="NavigationItem.RequiredPermissions"/> are filtered
    /// on the frontend based on the JWT permission claims for UX; the API also
    /// enforces permissions at the controller layer (OWASP A01 — defence in depth).
    /// </summary>
    [HttpGet("navigation/admin")]
    [RequirePermission("platform:info:read")]
    public IActionResult GetAdminNavigation()
        => Ok(BuildNavigationTree(UiContext.Admin));

    /// <summary>
    /// Returns the complete navigation tree for the <b>Operation UI</b> shell.
    /// </summary>
    [HttpGet("navigation/operation")]
    [RequirePermission("platform:info:read")]
    public IActionResult GetOperationNavigation()
        => Ok(BuildNavigationTree(UiContext.Operation));

    // ── Private Helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Builds a hierarchical navigation tree for the given <paramref name="context"/>.
    /// Items with a <see cref="NavigationItem.ParentKey"/> are nested under their parent.
    /// Top-level items (ParentKey is null) become the tree roots.
    /// </summary>
    private object BuildNavigationTree(UiContext context)
    {
        var items = registry.GetNavigationItems(context).ToList();

        // Index all items by key for O(1) parent lookup
        var lookup = items.ToDictionary(n => n.Key);

        // Recursively build child list per item
        var children = items
            .Where(n => n.ParentKey is not null && lookup.ContainsKey(n.ParentKey))
            .GroupBy(n => n.ParentKey!)
            .ToDictionary(g => g.Key, g => g.OrderBy(n => n.Order).ToList());

        // Only top-level roots (null parent OR parent not in same context)
        var roots = items
            .Where(n => n.ParentKey is null || !lookup.ContainsKey(n.ParentKey))
            .OrderBy(n => n.Order);

        return roots.Select(root => ToNode(root, children)).ToList();
    }

    private static object ToNode(
        NavigationItem item,
        Dictionary<string, List<NavigationItem>> children)
        => new
        {
            item.Key,
            item.Label,
            item.Route,
            item.Icon,
            item.Order,
            item.IsGroup,
            item.Badge,
            item.RequiredPermissions,
            item.ModuleId,
            Children = children.TryGetValue(item.Key, out var kids)
                ? kids.Select(c => ToNode(c, children)).ToArray()
                : [],
        };
}


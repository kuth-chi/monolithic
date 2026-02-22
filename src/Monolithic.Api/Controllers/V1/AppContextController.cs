using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Identity.Application;
using Monolithic.Api.Modules.Platform.Contracts;
using Monolithic.Api.Modules.Platform.Core.Abstractions;
using Monolithic.Api.Modules.Platform.Core.Infrastructure;

namespace Monolithic.Api.Controllers.V1;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Core Foundation OS-Like App Context — 3-Key Validator Endpoint.
///
/// <c>GET /api/v1/platform/app-context</c> is the single entry-point called by
/// every UI shell on mount. It validates all three context keys from the JWT and
/// returns the complete bootstrapping payload in one round-trip:
///
///   Key 1 — User        : identity claims from validated JWT
///   Key 2 — Business    : active tenant context from JWT business_id claim
///   Key 3 — Authorization : role + permission claims → which UIs are accessible
///
/// The navigation trees for both Admin and Operation shells are computed from
/// <see cref="ModuleRegistry"/> and filtered server-side to only items the
/// caller has permission to see (OWASP A01 defence-in-depth; the actual API
/// endpoints are still individually guarded by [RequirePermission]).
///
/// Performance: reads exclusively from the validated JWT (zero DB queries).
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/v1/platform")]
[Authorize]
public sealed class AppContextController(
    ModuleRegistry registry,
    ITenantContext tenantContext) : ControllerBase
{
    private const string FullAccessPermission = "*:full";

    // Admin UI requires at least one admin-scoped permission
    private static readonly string[] AdminAccessPermissions =
    [
        "platform:info:read",
        "users:read",
        "users:roles:read",
    ];

    // ── GET /api/v1/platform/app-context ─────────────────────────────────────

    /// <summary>
    /// Returns the validated 3-key context for the authenticated caller.
    /// Called once per shell mount; cached client-side for the session lifetime.
    ///
    /// HTTP 401 — no valid JWT present.
    /// HTTP 200 — all three keys validated; payload contains both navigation trees.
    /// </summary>
    [HttpGet("app-context")]
    [ProducesResponseType(typeof(AppContextResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetAppContext()
    {
        // ── Key 1: User ───────────────────────────────────────────────────────
        var userId = tenantContext.UserId;
        if (userId is null)
            return Unauthorized(new { message = "Invalid or expired token." });

        var email    = User.FindFirstValue(ClaimTypes.Email)
                    ?? User.FindFirstValue("email")
                    ?? string.Empty;
        var fullName = User.FindFirstValue(ClaimTypes.Name)
                    ?? User.FindFirstValue("name")
                    ?? string.Empty;

        var userCtx = new UserContextDto(userId.Value, email, fullName);

        // ── Key 2: Business ───────────────────────────────────────────────────
        // businessId read exclusively from the JWT (OWASP A01 — no tenant spoofing).
        // All memberships for the business-switcher → GET /api/v1/auth/me.
        BusinessContextDto? businessCtx = null;
        if (tenantContext.HasBusiness)
        {
            var businessName = User.FindFirstValue(AppClaimTypes.BusinessName) ?? string.Empty;

            businessCtx = new BusinessContextDto(
                BusinessId: tenantContext.BusinessId!.Value,
                Name:       businessName,
                Code:       null,  // not in current JWT schema; extend AppClaimTypes if needed
                IsDefault:  true);
        }

        // ── Key 3: Authorization ─────────────────────────────────────────────
        var roles       = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var permissions = User.FindAll(AppClaimTypes.Permission).Select(c => c.Value).ToList();
        var hasFullAccess   = permissions.Contains(FullAccessPermission);
        var canAccessAdmin  = hasFullAccess || AdminAccessPermissions.Any(permissions.Contains);
        var canAccessOp     = businessCtx is not null || hasFullAccess;

        var authCtx = new AuthContextDto(
            Roles:            roles.AsReadOnly(),
            Permissions:      permissions.AsReadOnly(),
            HasFullAccess:    hasFullAccess,
            CanAccessAdmin:   canAccessAdmin,
            CanAccessOperation: canAccessOp);

        // ── Navigation (permission-filtered for both shells) ─────────────────
        var adminNav = BuildFilteredTree(UiContext.Admin, permissions, hasFullAccess);
        var opNav    = BuildFilteredTree(UiContext.Operation, permissions, hasFullAccess);

        var navCtx = new NavigationContextDto(
            Admin:     adminNav,
            Operation: opNav);

        return Ok(new AppContextResponse(userCtx, businessCtx, authCtx, navCtx));
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Builds a permission-filtered hierarchical navigation tree for a given
    /// <paramref name="uiContext"/> from the live <see cref="ModuleRegistry"/>.
    /// </summary>
    private IReadOnlyList<NavigationNodeDto> BuildFilteredTree(
        UiContext uiContext,
        List<string> userPermissions,
        bool hasFullAccess)
    {
        var allItems = registry.GetNavigationItems(uiContext).ToList();

        // Filter to items the caller has permission to see
        var visible = allItems
            .Where(item => hasFullAccess
                           || item.RequiredPermissions is null
                           || item.RequiredPermissions.Length == 0
                           || item.RequiredPermissions.Any(userPermissions.Contains))
            .ToList();

        var visibleKeys = visible.Select(i => i.Key).ToHashSet();

        // Build child map (only for visible items)
        var childrenMap = visible
            .Where(i => i.ParentKey is not null && visibleKeys.Contains(i.ParentKey))
            .GroupBy(i => i.ParentKey!)
            .ToDictionary(g => g.Key, g => g.OrderBy(i => i.Order).ToList());

        // Roots = no parent, or parent not in visible set
        var roots = visible
            .Where(i => i.ParentKey is null || !visibleKeys.Contains(i.ParentKey))
            .OrderBy(i => i.Order);

        return roots.Select(r => MapToNode(r, childrenMap)).ToList().AsReadOnly();
    }

    private static NavigationNodeDto MapToNode(
        NavigationItem item,
        Dictionary<string, List<NavigationItem>> childrenMap)
        => new(
            Key:      item.Key,
            Label:    item.Label,
            Route:    item.Route,
            Icon:     item.Icon,
            Order:    item.Order,
            IsGroup:  item.IsGroup,
            Badge:    item.Badge,
            ModuleId: item.ModuleId,
            Children: childrenMap.TryGetValue(item.Key, out var kids)
                ? kids.Select(c => MapToNode(c, childrenMap)).ToList().AsReadOnly()
                : (IReadOnlyList<NavigationNodeDto>)[]);
}


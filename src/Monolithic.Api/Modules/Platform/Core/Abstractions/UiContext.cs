namespace Monolithic.Api.Modules.Platform.Core.Abstractions;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Identifies which UI shell a navigation item or module feature belongs to.
///
/// Design pattern — two distinct UI layouts driven by intent:
///
///  ┌──────────────────────────────────────────────────────────────┐
///  │  Admin UI — platform/system administration                    │
///  │  • User management, roles, identity configuration            │
///  │  • Feature flags, themes, templates, notification settings   │
///  │  • Business licensing, multi-branch setup                    │
///  │  • System health, audit logs, module catalog                 │
///  └──────────────────────────────────────────────────────────────┘
///  ┌──────────────────────────────────────────────────────────────┐
///  │  Operation UI — daily business operations                     │
///  │  • Sales, purchases, inventory, finance, HR                  │
///  │  • Customer/vendor management                                │
///  │  • Dashboards, reports, approvals, workflows                 │
///  └──────────────────────────────────────────────────────────────┘
///
/// Modules declare which context(s) their navigation items belong to via
/// <see cref="IModule.GetNavigationItems"/>. The frontend fetches the manifest
/// from <c>GET /api/v1/platform/navigation?context=admin|operation</c> and
/// builds its sidebar dynamically — no frontend code change needed when a new
/// module is installed.
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public enum UiContext
{
    /// <summary>Shown only in the Administrator UI shell.</summary>
    Admin = 1,

    /// <summary>Shown only in the Operations UI shell.</summary>
    Operation = 2,

    /// <summary>Shown in both UI shells (e.g. user profile, notifications).</summary>
    Both = 3,
}

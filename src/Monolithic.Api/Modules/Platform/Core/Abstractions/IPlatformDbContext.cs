using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Modules.Platform.FeatureFlags.Domain;
using Monolithic.Api.Modules.Platform.Notifications.Domain;
using Monolithic.Api.Modules.Platform.Templates.Domain;
using Monolithic.Api.Modules.Platform.Themes.Domain;
using Monolithic.Api.Modules.Platform.UserPreferences.Domain;

namespace Monolithic.Api.Modules.Platform.Core.Abstractions;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Database contract for Platform Foundation's own isolated database.
///
/// All Platform services depend on this interface rather than the concrete
/// <c>PlatformDbContext</c>. This allows unit-testing services without a real DB
/// and keeps Platform infrastructure details out of other modules.
///
/// Registered via DI as:
///   services.AddScoped&lt;IPlatformDbContext, PlatformDbContext&gt;()
/// inside <c>PlatformModuleRegistration</c>.
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public interface IPlatformDbContext
{
    // ── Templates ─────────────────────────────────────────────────────────────
    DbSet<TemplateDefinition> TemplateDefinitions { get; }
    DbSet<TemplateVersion>    TemplateVersions    { get; }

    // ── Themes ────────────────────────────────────────────────────────────────
    DbSet<ThemeProfile> ThemeProfiles { get; }

    // ── User Preferences ──────────────────────────────────────────────────────
    DbSet<UserPreference> UserPreferences { get; }

    // ── Feature Flags ─────────────────────────────────────────────────────────
    DbSet<FeatureFlag> FeatureFlags { get; }

    // ── Notifications ─────────────────────────────────────────────────────────
    DbSet<NotificationLog> NotificationLogs { get; }

    // ── Unit-of-Work ──────────────────────────────────────────────────────────
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

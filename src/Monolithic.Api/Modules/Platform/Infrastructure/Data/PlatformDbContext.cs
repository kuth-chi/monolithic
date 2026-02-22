using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Modules.Platform.Core.Abstractions;
using Monolithic.Api.Modules.Platform.FeatureFlags.Domain;
using Monolithic.Api.Modules.Platform.Notifications.Domain;
using Monolithic.Api.Modules.Platform.Templates.Domain;
using Monolithic.Api.Modules.Platform.Themes.Domain;
using Monolithic.Api.Modules.Platform.UserPreferences.Domain;

namespace Monolithic.Api.Modules.Platform.Infrastructure.Data;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Isolated EF Core DbContext for Platform Foundation.
///
/// Owns these entities:
///   • TemplateDefinition / TemplateVersion  — Scriban template engine
///   • ThemeProfile                          — design-token theming
///   • UserPreference                        — dashboard/widget layout per user
///   • FeatureFlag                           — per-business / per-user toggles
///   • NotificationLog                       — outbound message audit trail
///
/// Connection string key (resolved by ModuleDatabaseInitializer):
///   "Infrastructure:Databases:Platform"
///
/// Migrations folder: Migrations/Platform/
///   dotnet ef migrations add Init --context PlatformDbContext --output-dir Migrations/Platform
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class PlatformDbContext : DbContext, IPlatformDbContext
{
    public PlatformDbContext(DbContextOptions<PlatformDbContext> options) : base(options) { }

    // ── DbSets ────────────────────────────────────────────────────────────────

    public DbSet<TemplateDefinition> TemplateDefinitions => Set<TemplateDefinition>();
    public DbSet<TemplateVersion>    TemplateVersions    => Set<TemplateVersion>();
    public DbSet<ThemeProfile>       ThemeProfiles       => Set<ThemeProfile>();
    public DbSet<UserPreference>     UserPreferences     => Set<UserPreference>();
    public DbSet<FeatureFlag>        FeatureFlags        => Set<FeatureFlag>();
    public DbSet<NotificationLog>    NotificationLogs    => Set<NotificationLog>();

    // ── Model Configuration ───────────────────────────────────────────────────

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── TemplateDefinition + TemplateVersion ───────────────────────────────

        modelBuilder.Entity<TemplateDefinition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Slug).HasMaxLength(200).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(300).IsRequired();
            entity.Property(e => e.AvailableVariables).HasMaxLength(2000);
            entity.HasIndex(e => new { e.Slug, e.Scope, e.BusinessId, e.UserId }).IsUnique();
            entity.HasMany(e => e.Versions)
                  .WithOne(v => v.Definition)
                  .HasForeignKey(v => v.TemplateDefinitionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TemplateVersion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.VersionLabel).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ChangeNotes).HasMaxLength(1000);
        });

        // ── ThemeProfile ───────────────────────────────────────────────────────

        modelBuilder.Entity<ThemeProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => new { e.BusinessId, e.Name }).IsUnique();
            entity.Property(e => e.ColorPrimary).HasMaxLength(20);
            entity.Property(e => e.ColorSecondary).HasMaxLength(20);
            entity.Property(e => e.ColorAccent).HasMaxLength(20);
            entity.Property(e => e.FontFamily).HasMaxLength(200);
        });

        // ── UserPreference ─────────────────────────────────────────────────────

        modelBuilder.Entity<UserPreference>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.BusinessId }).IsUnique();
            entity.Property(e => e.ColorScheme).HasMaxLength(20);
            entity.Property(e => e.PreferredLocale).HasMaxLength(20);
            entity.Property(e => e.PreferredTimezone).HasMaxLength(60);
        });

        // ── FeatureFlag ────────────────────────────────────────────────────────

        modelBuilder.Entity<FeatureFlag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).HasMaxLength(200).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(300);
            entity.HasIndex(e => new { e.Key, e.Scope, e.BusinessId, e.UserId }).IsUnique();
        });

        // ── NotificationLog ────────────────────────────────────────────────────

        modelBuilder.Entity<NotificationLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Recipient).HasMaxLength(500).IsRequired();
            entity.Property(e => e.TemplateSl).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Subject).HasMaxLength(500);
            entity.HasIndex(e => new { e.BusinessId, e.CreatedAtUtc });
            entity.HasIndex(e => new { e.UserId, e.Status });
        });
    }
}

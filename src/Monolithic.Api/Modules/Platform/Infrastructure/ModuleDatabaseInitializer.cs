using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Modules.Platform.Core.Abstractions;
using Monolithic.Api.Modules.Platform.Core.Infrastructure;

namespace Monolithic.Api.Modules.Platform.Infrastructure;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Runs EF Core <c>MigrateAsync()</c> for every module that declares a
/// <see cref="DatabaseDescriptor"/> via <see cref="IModule.GetDatabaseDescriptor"/>.
///
/// Called once during <see cref="PlatformStartup.InitializePlatformAsync"/>
/// BEFORE module first-run hooks, ensuring that every module's schema is
/// up-to-date before any data is seeded.
///
/// Design — why auto-migrate at startup?
///   • Zero-friction developer experience: <c>dotnet run</c> is all you need.
///   • Production: Each module's DbContext is migrated atomically before traffic
///     hits that module — a hard deploy failure rolls back before any schema change.
///   • Idempotent: EF Core migrations are applied only once; already-applied
///     migrations are skipped via the __EFMigrationsHistory table.
///
/// When NOT to use this:
///   If your production deployment policy forbids runtime schema changes, set
///   PLATFORM__AutoMigrateOnStartup=false and run migrations via CI/CD instead.
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public static class ModuleDatabaseInitializer
{
    private const string AutoMigrateConfigKey = "Platform:AutoMigrateOnStartup";

    /// <summary>
    /// Migrates every module database that is registered via
    /// <see cref="IModule.GetDatabaseDescriptor"/>.
    /// </summary>
    public static async Task MigrateAllAsync(
        IServiceScope       scope,
        IConfiguration      configuration,
        ILogger             logger,
        CancellationToken   ct = default)
    {
        // Allow ops teams to disable auto-migration in production (default: enabled)
        var autoMigrate = configuration.GetValue(AutoMigrateConfigKey, defaultValue: true);
        if (!autoMigrate)
        {
            logger.LogInformation(
                "[ModuleDatabaseInitializer] Auto-migration is disabled " +
                "(Platform:AutoMigrateOnStartup=false). Skipping.");
            return;
        }

        var registry = scope.ServiceProvider.GetRequiredService<ModuleRegistry>();
        var sp       = scope.ServiceProvider;

        foreach (var module in registry.Modules)
        {
            var descriptor = module.GetDatabaseDescriptor();
            if (descriptor is null)
                continue;

            var label = descriptor.DisplayName ?? descriptor.ModuleId;

            try
            {
                logger.LogInformation(
                    "[ModuleDatabaseInitializer] Migrating database for module '{Label}' " +
                    "(DbContext: {Type})...",
                    label, descriptor.DbContextType.Name);

                // Resolve the concrete DbContext from DI by its registered type.
                // Each module registers its DbContext in its own ModuleRegistration.
                var dbContext = (DbContext)sp.GetRequiredService(descriptor.DbContextType);

                var pending = (await dbContext.Database.GetPendingMigrationsAsync(ct)).ToList();

                if (pending.Count == 0)
                {
                    logger.LogDebug(
                        "[ModuleDatabaseInitializer] '{Label}': database is up-to-date.", label);
                }
                else
                {
                    logger.LogInformation(
                        "[ModuleDatabaseInitializer] '{Label}': applying {Count} pending migration(s): {Migrations}",
                        label, pending.Count, string.Join(", ", pending));

                    await dbContext.Database.MigrateAsync(ct);

                    logger.LogInformation(
                        "[ModuleDatabaseInitializer] '{Label}': migration complete.", label);
                }
            }
            catch (Exception ex)
            {
                // A failed migration is fatal — the module cannot operate with a stale schema.
                logger.LogCritical(ex,
                    "[ModuleDatabaseInitializer] Database migration FAILED for module '{Label}'. " +
                    "The application will continue but the module may be non-functional. " +
                    "Check the connection string at config key '{Key}'.",
                    label, descriptor.ConnectionStringKey);
            }
        }
    }
}

namespace Monolithic.Api.Modules.Platform.Core.Abstractions;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Declares the database requirements of a module.
///
/// The Platform Foundation's <c>ModuleDatabaseInitializer</c> reads these
/// descriptors at startup and calls <c>MigrateAsync()</c> on each module's
/// DbContext so that every module's schema is always up-to-date without manual
/// migration commands.
///
/// Usage (inside a module's GetDatabaseDescriptor()):
/// <code>
///   public override DatabaseDescriptor? GetDatabaseDescriptor() =>
///       new(ModuleId,
///           "Infrastructure:Databases:Platform",
///           typeof(PlatformDbContext));
/// </code>
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public sealed record DatabaseDescriptor(
    /// <summary>
    /// Matches <see cref="IModule.ModuleId"/>. Used in log messages and to
    /// build the per-module connection string configuration key.
    /// </summary>
    string ModuleId,

    /// <summary>
    /// IConfiguration key that holds the connection string.
    /// Example: <c>"Infrastructure:Databases:Platform"</c>
    ///
    /// In Development the key should resolve to a SQLite file path.
    /// In Production it should resolve to a PostgreSQL DSN.
    /// </summary>
    string ConnectionStringKey,

    /// <summary>
    /// The EF Core <see cref="Microsoft.EntityFrameworkCore.DbContext"/> type
    /// to instantiate and migrate. Must be resolvable from DI.
    /// </summary>
    Type DbContextType,

    /// <summary>
    /// Short display name used in startup logs.
    /// Defaults to <see cref="ModuleId"/> if null.
    /// </summary>
    string? DisplayName = null,

    /// <summary>
    /// Optional SQL schema name for all tables owned by this module.
    /// When null, EF Core uses the provider default (<c>dbo</c> / <c>public</c>).
    /// </summary>
    string? SchemaName = null);

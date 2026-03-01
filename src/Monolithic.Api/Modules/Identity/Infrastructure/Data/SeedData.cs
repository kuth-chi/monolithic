using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using BusinessDomain = Monolithic.Api.Modules.Business.Domain;
using Monolithic.Api.Modules.Identity.Domain;

namespace Monolithic.Api.Modules.Identity.Infrastructure.Data;

public static class SeedData
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");

        logger.LogInformation("[Seed] ══════════════════════════════════════════");
        logger.LogInformation("[Seed] Database seeding started.");
        logger.LogInformation("[Seed] ══════════════════════════════════════════");

        // Schema is managed via EF Core migrations run in Program.cs (MigrateAsync).
        // EnsureCreatedAsync is intentionally omitted to avoid bypassing the migration history.
        logger.LogInformation("[Seed] Schema managed by EF migrations (Program.cs).");

        // Optional destructive bootstrap reset for local/dev re-install scenarios.
        // Guarded by explicit config + confirmation token to avoid accidental data loss.
        await TryResetBootstrapDataAsync(context, configuration, logger);

        // ── Step 1: Permissions ───────────────────────────────────────────────
        logger.LogInformation("[Seed] Step 1/6 — Permissions");
        var permissions = await SeedPermissionsAsync(context, logger);

        // ── Step 2: Roles ─────────────────────────────────────────────────────
        logger.LogInformation("[Seed] Step 2/6 — Roles");
        await SeedRolesAsync(roleManager, context, permissions, logger);

        // ── Step 3-6: Seed policy (disabled for users/business/licenses) ──────
        // Keep these tables empty on startup:
        //   • AspNetUsers (users)
        //   • Businesses (companies)
        //   • BusinessLicenses (licenses)
        // Data for these tables must be created only through runtime app flows
        // (registration/business creation/license activation), not startup seeding.
        logger.LogInformation(
            "[Seed] Steps 3-6 skipped by policy — startup pre-seeding for users, businesses, and licenses is disabled.");

        logger.LogInformation("[Seed] ══════════════════════════════════════════");
        logger.LogInformation("[Seed] Database seeding completed successfully.");
        logger.LogInformation("[Seed] ══════════════════════════════════════════");
    }

    private static async Task TryResetBootstrapDataAsync(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger logger)
    {
        var enableReset = configuration.GetValue("Seed:Bootstrap:EnableReset", false);
        if (!enableReset) return;

        var providedToken = configuration["Seed:Bootstrap:ResetToken"];
        var expectedToken = "RESET_LOCAL_BOOTSTRAP_DATA";

        if (!string.Equals(providedToken, expectedToken, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "[Seed] Bootstrap reset requested but token invalid. " +
                "Set Seed:Bootstrap:ResetToken to the expected value to proceed.");
            return;
        }

        logger.LogWarning("[Seed] Destructive bootstrap reset started (demo users/businesses/licenses only).");

        var bootstrapEmails = new[]
        {
            "admin@example.com",
            "sysadmin@example.com",
            "accountant@example.com",
            "sales@example.com",
            "user1@example.com"
        };

        var bootstrapBusinessNames = new[] { "ABC Group", "XYZ Tech" };

        var normalizedEmails = bootstrapEmails
            .Select(e => e.Trim().ToLowerInvariant())
            .ToHashSet();

        var userIds = await context.Users
            .Where(u => u.Email != null && normalizedEmails.Contains(u.Email.ToLower()))
            .Select(u => u.Id)
            .ToListAsync();

        if (userIds.Count > 0)
        {
            var memberships = await context.UserBusinesses
                .Where(ub => userIds.Contains(ub.UserId))
                .ToListAsync();
            if (memberships.Count > 0)
            {
                context.UserBusinesses.RemoveRange(memberships);
                await context.SaveChangesAsync();
            }

            var ownerships = await context.BusinessOwnerships
                .Where(o => userIds.Contains(o.OwnerId))
                .ToListAsync();
            if (ownerships.Count > 0)
            {
                context.BusinessOwnerships.RemoveRange(ownerships);
                await context.SaveChangesAsync();
            }

            var licenses = await context.BusinessLicenses
                .Where(l => userIds.Contains(l.OwnerId))
                .ToListAsync();
            if (licenses.Count > 0)
            {
                context.BusinessLicenses.RemoveRange(licenses);
                await context.SaveChangesAsync();
            }

            await context.HardDeleteWhereAsync<ApplicationUser>(
                u => u.Email != null && normalizedEmails.Contains(u.Email.ToLower()));
        }

        // Hard-delete only bootstrap demo businesses by name.
        await context.HardDeleteWhereAsync<BusinessDomain.Business>(
            b => bootstrapBusinessNames.Contains(b.Name));

        logger.LogWarning("[Seed] Destructive bootstrap reset completed.");
    }

    private static async Task<Dictionary<string, Permission>> SeedPermissionsAsync(
        ApplicationDbContext context,
        ILogger logger)
    {
        // Keep permission catalog in sync on every startup.
        // This is intentionally additive (upsert-by-name): existing rows are preserved,
        // missing keys are inserted so newly introduced permissions appear in older DBs.
        var permissions = new List<Permission>
        {
            // Owner permissions (full access)
            new() { Id = Guid.NewGuid(), Name = "*:full", Description = "Full system access" },

            // Owner / admin
            new() { Id = Guid.NewGuid(), Name = "owner:read", Description = "Read owner dashboard and owned businesses" },
            new() { Id = Guid.NewGuid(), Name = "owner:write", Description = "Create/revoke owned businesses" },
            new() { Id = Guid.NewGuid(), Name = "admin:write", Description = "Administrative write access" },

            // Business management
            new() { Id = Guid.NewGuid(), Name = "business:read", Description = "Read business settings, branches, and policies" },
            new() { Id = Guid.NewGuid(), Name = "business:write", Description = "Write business settings, branches, and policies" },
            new() { Id = Guid.NewGuid(), Name = "business:admin", Description = "Administrative business operations" },

            // User management
            new() { Id = Guid.NewGuid(), Name = "users:read", Description = "Read user data" },
            new() { Id = Guid.NewGuid(), Name = "users:create", Description = "Create users" },
            new() { Id = Guid.NewGuid(), Name = "users:update", Description = "Update users" },
            new() { Id = Guid.NewGuid(), Name = "users:delete", Description = "Delete users" },

            // Employee management
            new() { Id = Guid.NewGuid(), Name = "employees:read", Description = "Read employee data" },
            new() { Id = Guid.NewGuid(), Name = "employees:create", Description = "Create employees" },
            new() { Id = Guid.NewGuid(), Name = "employees:update", Description = "Update employees" },
            new() { Id = Guid.NewGuid(), Name = "employees:delete", Description = "Delete employees" },

            // Sales management
            new() { Id = Guid.NewGuid(), Name = "sales:read", Description = "Read sales data" },
            new() { Id = Guid.NewGuid(), Name = "sales:create", Description = "Create sales orders" },
            new() { Id = Guid.NewGuid(), Name = "sales:update", Description = "Update sales orders" },
            new() { Id = Guid.NewGuid(), Name = "sales:delete", Description = "Delete sales orders" },
            new() { Id = Guid.NewGuid(), Name = "sales:write", Description = "Create/update/delete sales documents" },

            // Accounting
            new() { Id = Guid.NewGuid(), Name = "accounting:read", Description = "Read financial/accounting data" },
            new() { Id = Guid.NewGuid(), Name = "accounting:create", Description = "Create accounting entries" },
            new() { Id = Guid.NewGuid(), Name = "accounting:update", Description = "Update accounting entries" },
            new() { Id = Guid.NewGuid(), Name = "accounting:delete", Description = "Delete accounting entries" },

            // Finance (module-level aliases used by V1 controllers)
            new() { Id = Guid.NewGuid(), Name = "finance:read", Description = "Read finance and accounting data" },
            new() { Id = Guid.NewGuid(), Name = "finance:write", Description = "Create/update finance data" },
            new() { Id = Guid.NewGuid(), Name = "finance:admin", Description = "Administrative finance operations" },

            // Costing
            new() { Id = Guid.NewGuid(), Name = "costing:read", Description = "Read costing setups and ledgers" },
            new() { Id = Guid.NewGuid(), Name = "costing:write", Description = "Create/update costing setups" },

            // Inventory
            new() { Id = Guid.NewGuid(), Name = "inventory:read", Description = "Read inventory data" },
            new() { Id = Guid.NewGuid(), Name = "inventory:create", Description = "Create inventory items" },
            new() { Id = Guid.NewGuid(), Name = "inventory:update", Description = "Update inventory items" },
            new() { Id = Guid.NewGuid(), Name = "inventory:delete", Description = "Delete inventory items" },

            // Customers & Vendors
            new() { Id = Guid.NewGuid(), Name = "customers:read", Description = "Read customer data" },
            new() { Id = Guid.NewGuid(), Name = "customers:create", Description = "Create customers" },
            new() { Id = Guid.NewGuid(), Name = "customers:update", Description = "Update customers" },
            new() { Id = Guid.NewGuid(), Name = "customers:delete", Description = "Delete customers" },
            new() { Id = Guid.NewGuid(), Name = "vendors:read", Description = "Read vendor data" },
            new() { Id = Guid.NewGuid(), Name = "vendors:create", Description = "Create vendors" },
            new() { Id = Guid.NewGuid(), Name = "vendors:update", Description = "Update vendors" },
            new() { Id = Guid.NewGuid(), Name = "vendors:delete", Description = "Delete vendors" },

            // Bank account management
            new() { Id = Guid.NewGuid(), Name = "bankaccounts:read", Description = "Read bank account data" },
            new() { Id = Guid.NewGuid(), Name = "bankaccounts:create", Description = "Create bank accounts" },
            new() { Id = Guid.NewGuid(), Name = "bankaccounts:update", Description = "Update bank accounts" },
            new() { Id = Guid.NewGuid(), Name = "bankaccounts:delete", Description = "Delete bank accounts" },

            // Purchase management
            new() { Id = Guid.NewGuid(), Name = "purchase:read", Description = "Read purchase orders" },
            new() { Id = Guid.NewGuid(), Name = "purchase:create", Description = "Create purchase orders" },
            new() { Id = Guid.NewGuid(), Name = "purchase:update", Description = "Update purchase orders" },
            new() { Id = Guid.NewGuid(), Name = "purchase:delete", Description = "Delete purchase orders" },
            new() { Id = Guid.NewGuid(), Name = "purchase:write", Description = "Create/update purchase operations" },

            // Purchasing workflow (estimate PO)
            new() { Id = Guid.NewGuid(), Name = "purchasing:read", Description = "Read estimate purchase orders" },
            new() { Id = Guid.NewGuid(), Name = "purchasing:write", Description = "Create/update estimate purchase orders" },
            new() { Id = Guid.NewGuid(), Name = "purchasing:approve", Description = "Approve/convert estimate purchase orders" },

            // Analytics & Dashboards
            new() { Id = Guid.NewGuid(), Name = "analytics:read", Description = "Read analytics and dashboards" },
            new() { Id = Guid.NewGuid(), Name = "reports:read", Description = "Read reports" },
            new() { Id = Guid.NewGuid(), Name = "reports:export", Description = "Export reports" },

            // Warehouse management
            new() { Id = Guid.NewGuid(), Name = "warehouse:read", Description = "Read warehouse and location data" },
            new() { Id = Guid.NewGuid(), Name = "warehouse:create", Description = "Create warehouses and locations" },
            new() { Id = Guid.NewGuid(), Name = "warehouse:update", Description = "Update warehouses and locations" },
            new() { Id = Guid.NewGuid(), Name = "warehouse:delete", Description = "Delete warehouses and locations" },

            // Platform Foundation
            new() { Id = Guid.NewGuid(), Name = "platform:info:read",          Description = "View platform module info and widget catalog" },
            new() { Id = Guid.NewGuid(), Name = "platform:templates:read",     Description = "View communication templates" },
            new() { Id = Guid.NewGuid(), Name = "platform:templates:write",    Description = "Create and edit communication templates" },
            new() { Id = Guid.NewGuid(), Name = "platform:themes:read",        Description = "View theme profiles" },
            new() { Id = Guid.NewGuid(), Name = "platform:themes:write",       Description = "Create and edit theme profiles" },
            new() { Id = Guid.NewGuid(), Name = "platform:preferences:read",   Description = "View user preferences and dashboard layouts" },
            new() { Id = Guid.NewGuid(), Name = "platform:preferences:write",  Description = "Update user preferences and dashboard layouts" },
            new() { Id = Guid.NewGuid(), Name = "platform:preferences:admin",  Description = "Administer user preferences" },
            new() { Id = Guid.NewGuid(), Name = "platform:feature-flags:read", Description = "View feature flags" },
            new() { Id = Guid.NewGuid(), Name = "platform:feature-flags:write", Description = "Update and create feature flags" },
            new() { Id = Guid.NewGuid(), Name = "platform:notifications:read",  Description = "View notification logs" },
            new() { Id = Guid.NewGuid(), Name = "platform:notifications:write", Description = "Send notifications" },

            // Role catalog
            new() { Id = Guid.NewGuid(), Name = "users:roles:read", Description = "View role catalog" },
            new() { Id = Guid.NewGuid(), Name = "users:roles:admin", Description = "Manage roles and permissions" },

            // User profiles — admin-elevated
            new() { Id = Guid.NewGuid(), Name = "users:profiles:read",   Description = "Read any user profile (admin/manager)" },
            new() { Id = Guid.NewGuid(), Name = "users:profiles:write",  Description = "Edit any user profile (admin override)" },
            new() { Id = Guid.NewGuid(), Name = "users:profiles:delete", Description = "Deactivate user accounts (admin)" },

            // User profiles — self-data (ABAC ownership-scoped)
            // Holders may access ONLY their own profile; enforced at runtime
            // by SelfOwnershipAuthorizationHandler.
            new() { Id = Guid.NewGuid(), Name = "users:profiles:self", Description = "View and edit own user profile", IsSelfScoped = true }
        };

        foreach (var permission in permissions)
        {
            ApplyDerivedPermissionMetadata(permission);
        }

        var existing = await context.Permissions
            .AsNoTracking()
            .Select(p => p.Name)
            .ToListAsync();

        var existingSet = existing.ToHashSet(StringComparer.Ordinal);
        var missing = permissions
            .Where(p => !existingSet.Contains(p.Name))
            .ToList();

        if (missing.Count > 0)
        {
            logger.LogInformation("[Seed]   Permissions — inserting {Count} new permission(s): {Names}",
                missing.Count, string.Join(", ", missing.Select(p => p.Name)));
            await context.Permissions.AddRangeAsync(missing);
            await context.SaveChangesAsync();
        }
        else
        {
            logger.LogInformation("[Seed]   Permissions — all {Total} permission(s) already present, skipped.",
                permissions.Count);
        }

        // Backfill metadata for existing rows created before grouped permission model.
        var existingRows = await context.Permissions.ToListAsync();
        var hasMetadataUpdates = false;

        foreach (var permission in existingRows)
        {
            var originalSource = permission.Source;
            var originalGroup = permission.GroupName;
            var originalFeature = permission.FeatureName;
            var originalAction = permission.ActionName;

            ApplyDerivedPermissionMetadata(permission);

            if (!string.Equals(originalSource, permission.Source, StringComparison.Ordinal) ||
                !string.Equals(originalGroup, permission.GroupName, StringComparison.Ordinal) ||
                !string.Equals(originalFeature, permission.FeatureName, StringComparison.Ordinal) ||
                !string.Equals(originalAction, permission.ActionName, StringComparison.Ordinal))
            {
                hasMetadataUpdates = true;
            }

            // Backfill IsSelfScoped for :self permissions created before this field existed
            if (!permission.IsSelfScoped
                && permission.Name.EndsWith(":self", StringComparison.OrdinalIgnoreCase))
            {
                permission.IsSelfScoped = true;
                hasMetadataUpdates = true;
            }
        }

        if (hasMetadataUpdates)
        {
            logger.LogInformation("[Seed]   Permissions — backfilled metadata on existing rows.");
            await context.SaveChangesAsync();
        }

        var total = await context.Permissions.CountAsync();
        logger.LogInformation("[Seed]   Permissions — catalog total: {Total} permission(s).", total);
        return await context.Permissions.ToDictionaryAsync(p => p.Name);
    }

    private static void ApplyDerivedPermissionMetadata(Permission permission)
    {
        var segments = permission.Name
            .Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var source = string.IsNullOrWhiteSpace(permission.Source)
            ? segments.FirstOrDefault() ?? "system"
            : permission.Source;

        var featureSegment = segments.Length switch
        {
            >= 3 => segments[1],
            2 => segments[0],
            _ => "global"
        };

        var actionSegment = segments.Length switch
        {
            >= 2 => segments[^1],
            _ => "read"
        };

        permission.Source = NormalizeSegment(source);
        permission.GroupName = string.IsNullOrWhiteSpace(permission.GroupName)
            ? ToTitleCase(source)
            : permission.GroupName.Trim();
        permission.FeatureName = string.IsNullOrWhiteSpace(permission.FeatureName)
            ? ToTitleCase(featureSegment)
            : permission.FeatureName.Trim();
        permission.ActionName = string.IsNullOrWhiteSpace(permission.ActionName)
            ? NormalizeSegment(actionSegment)
            : NormalizeSegment(permission.ActionName);

        // Auto-derive IsSelfScoped from action token
        if (string.Equals(actionSegment, "self", StringComparison.OrdinalIgnoreCase))
            permission.IsSelfScoped = true;
    }

    private static string NormalizeSegment(string value)
        => value
            .Trim()
            .ToLowerInvariant()
            .Replace(' ', '-')
            .Replace('_', '-');

    private static string ToTitleCase(string value)
    {
        var cleaned = value
            .Trim()
            .Replace('-', ' ')
            .Replace('_', ' ')
            .ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(cleaned))
            return "General";

        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(cleaned);
    }

    private static async Task<(ApplicationRole ownerRole, ApplicationRole systemAdminRole, ApplicationRole staffRole, ApplicationRole userRole)> SeedRolesAsync(
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext context,
        Dictionary<string, Permission> permissions,
        ILogger logger)
    {
        // ── Protected / system roles ──────────────────────────────────────────────────
        ApplicationRole? ownerRole = await roleManager.FindByNameAsync(SystemRoleNames.Owner);
        if (ownerRole is null)
        {
            ownerRole = new ApplicationRole
            {
                Id = Guid.NewGuid(),
                Name = SystemRoleNames.Owner,
                Description = "Platform owner — full unrestricted access",
                IsSystemRole = true,
            };
            await roleManager.CreateAsync(ownerRole);
            logger.LogInformation("[Seed]   Role created  : {Role} (system-protected)", SystemRoleNames.Owner);
        }
        else
        {
            if (!ownerRole.IsSystemRole)
            {
                ownerRole.IsSystemRole = true;
                await roleManager.UpdateAsync(ownerRole);
                logger.LogInformation("[Seed]   Role patched  : {Role} — marked system-protected.", SystemRoleNames.Owner);
            }
            else
            {
                logger.LogInformation("[Seed]   Role skipped  : {Role} (already exists)", SystemRoleNames.Owner);
            }
        }

        ApplicationRole? systemAdminRole = await roleManager.FindByNameAsync(SystemRoleNames.SystemAdmin);
        if (systemAdminRole is null)
        {
            systemAdminRole = new ApplicationRole
            {
                Id = Guid.NewGuid(),
                Name = SystemRoleNames.SystemAdmin,
                Description = "System administrator — full platform management, cannot be deleted",
                IsSystemRole = true,
            };
            await roleManager.CreateAsync(systemAdminRole);
            logger.LogInformation("[Seed]   Role created  : {Role} (system-protected)", SystemRoleNames.SystemAdmin);
        }
        else
        {
            if (!systemAdminRole.IsSystemRole)
            {
                systemAdminRole.IsSystemRole = true;
                await roleManager.UpdateAsync(systemAdminRole);
                logger.LogInformation("[Seed]   Role patched  : {Role} — marked system-protected.", SystemRoleNames.SystemAdmin);
            }
            else
            {
                logger.LogInformation("[Seed]   Role skipped  : {Role} (already exists)", SystemRoleNames.SystemAdmin);
            }
        }

        // ── Regular roles ─────────────────────────────────────────────────────────────
        ApplicationRole? staffRole = await roleManager.FindByNameAsync("Staff");
        ApplicationRole? userRole = await roleManager.FindByNameAsync("User");

        if (staffRole is null)
        {
            staffRole = new ApplicationRole { Id = Guid.NewGuid(), Name = "Staff", Description = "Staff member with limited access", IsSystemRole = true };
            await roleManager.CreateAsync(staffRole);
            logger.LogInformation("[Seed]   Role created  : Staff (system-protected)");
        }
        else
        {
            if (!staffRole.IsSystemRole)
            {
                staffRole.IsSystemRole = true;
                await roleManager.UpdateAsync(staffRole);
                logger.LogInformation("[Seed]   Role patched  : Staff — marked system-protected.");
            }
            else
            {
                logger.LogInformation("[Seed]   Role skipped  : Staff (already exists)");
            }
        }

        if (userRole is null)
        {
            userRole = new ApplicationRole { Id = Guid.NewGuid(), Name = "User", Description = "Regular user with minimal access", IsSystemRole = true };
            await roleManager.CreateAsync(userRole);
            logger.LogInformation("[Seed]   Role created  : User (system-protected)");
        }
        else
        {
            if (!userRole.IsSystemRole)
            {
                userRole.IsSystemRole = true;
                await roleManager.UpdateAsync(userRole);
                logger.LogInformation("[Seed]   Role patched  : User — marked system-protected.");
            }
            else
            {
                logger.LogInformation("[Seed]   Role skipped  : User (already exists)");
            }
        }

        // Assign permissions to roles — both protected roles get full access
        await AssignPermissionsToRoleAsync(context, ownerRole, permissions, ["*:full"]);
        await AssignPermissionsToRoleAsync(context, systemAdminRole, permissions, ["*:full"]);
        await AssignPermissionsToRoleAsync(context, staffRole, permissions,
        [
            "users:read", "employees:read", "employees:create", "employees:update",
            "sales:read", "sales:create", "sales:update", "sales:write",
            "accounting:read", "finance:read", "finance:write",
            "inventory:read", "inventory:create", "inventory:update",
            "customers:read", "customers:create", "customers:update", "customers:delete",
            "vendors:read", "vendors:create", "vendors:update", "vendors:delete",
            "bankaccounts:read", "bankaccounts:create", "bankaccounts:update", "bankaccounts:delete",
            "purchase:read", "purchase:create", "purchase:update", "purchase:write",
            "purchasing:read", "purchasing:write",
            "business:read", "business:write",
            "costing:read", "costing:write",
            "warehouse:read", "warehouse:create", "warehouse:update",
            "analytics:read", "reports:read",
            // Self-data: Staff can view/edit their own profile
            "users:profiles:self",
        ]);
        await AssignPermissionsToRoleAsync(context, userRole, permissions,
        [
            "users:read",
            "sales:read",
            "customers:read",
            "purchase:read",
            "analytics:read", "reports:read",
            // Self-data: User can view/edit their own profile
            "users:profiles:self",
        ]);

        return (ownerRole, systemAdminRole, staffRole, userRole);
    }

    private static async Task AssignPermissionsToRoleAsync(
        ApplicationDbContext context,
        ApplicationRole role,
        Dictionary<string, Permission> permissions,
        string[] permissionNames)
    {
        var existingAssignments = context.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .Select(rp => rp.PermissionId)
            .ToList();

        foreach (var permissionName in permissionNames)
        {
            if (!permissions.TryGetValue(permissionName, out var permission))
                continue;

            if (!existingAssignments.Contains(permission.Id))
            {
                await context.RolePermissions.AddAsync(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permission.Id
                });
            }
        }

        await context.SaveChangesAsync();
    }

}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BusinessDomain = Monolithic.Api.Modules.Business.Domain;
using Monolithic.Api.Modules.Business.Contracts;
using Monolithic.Api.Modules.Identity.Domain;

namespace Monolithic.Api.Modules.Identity.Infrastructure.Data;

public static class SeedData
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");

        logger.LogInformation("[Seed] ══════════════════════════════════════════");
        logger.LogInformation("[Seed] Database seeding started.");
        logger.LogInformation("[Seed] ══════════════════════════════════════════");

        await context.Database.EnsureCreatedAsync();
        logger.LogInformation("[Seed] Schema verified / created.");

        // Optional destructive bootstrap reset for local/dev re-install scenarios.
        // Guarded by explicit config + confirmation token to avoid accidental data loss.
        await TryResetBootstrapDataAsync(context, configuration, logger);

        // ── Step 1: Permissions ───────────────────────────────────────────────
        logger.LogInformation("[Seed] Step 1/6 — Permissions");
        var permissions = await SeedPermissionsAsync(context, logger);

        // ── Step 2: Roles ─────────────────────────────────────────────────────
        logger.LogInformation("[Seed] Step 2/6 — Roles");
        var (ownerRole, systemAdminRole, staffRole, userRole) = await SeedRolesAsync(roleManager, context, permissions, logger);

        // ── Step 3: Demo users ────────────────────────────────────────────────
        logger.LogInformation("[Seed] Step 3/6 — Demo users");
        await SeedUsersAsync(userManager, ownerRole, systemAdminRole, staffRole, userRole, logger);

        // ── Step 4: Demo businesses + memberships ─────────────────────────────
        logger.LogInformation("[Seed] Step 4/6 — Demo businesses & memberships");
        await SeedBusinessesAndMembershipsAsync(context, userManager, logger);

        // ── Step 5: Owner license + ownership records ─────────────────────────
        logger.LogInformation("[Seed] Step 5/6 — Owner license & ownership");
        await SeedOwnershipAndLicenseAsync(context, userManager, logger);

        // ── Step 6: File / remote license mapping ─────────────────────────────
        logger.LogInformation("[Seed] Step 6/6 — License mapping (file / remote)");
        await ApplyLicenseMappingsFromFileAsync(
            context,
            userManager,
            roleManager,
            httpClientFactory,
            configuration,
            environment,
            logger);

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
        ApplicationRole? userRole  = await roleManager.FindByNameAsync("User");

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

    private static async Task SeedUsersAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationRole ownerRole,
        ApplicationRole systemAdminRole,
        ApplicationRole staffRole,
        ApplicationRole userRole,
        ILogger logger)
    {
        static ApplicationUser MakeUser(string email, string fullName) => new()
        {
            Id             = Guid.NewGuid(),
            Email          = email,
            UserName       = email,
            FullName       = fullName,
            EmailConfirmed = true,
            IsActive       = true,
            CreatedAtUtc   = DateTimeOffset.UtcNow
        };

        // Admin user — Owner
        if (await userManager.FindByEmailAsync("admin@example.com") is null)
        {
            var u = MakeUser("admin@example.com", "Admin User");
            await userManager.CreateAsync(u, "AdminPassword123!");
            await userManager.AddToRoleAsync(u, ownerRole.Name!);
            logger.LogInformation("[Seed]   User created  : {Email}  role={Role}", u.Email, ownerRole.Name);
        }
        else
        {
            logger.LogInformation("[Seed]   User skipped  : admin@example.com (already exists)");
        }

        // System administrator
        if (await userManager.FindByEmailAsync("sysadmin@example.com") is null)
        {
            var u = MakeUser("sysadmin@example.com", "System Administrator");
            await userManager.CreateAsync(u, "SysAdminPassword123!");
            await userManager.AddToRoleAsync(u, systemAdminRole.Name!);
            logger.LogInformation("[Seed]   User created  : {Email}  role={Role}", u.Email, systemAdminRole.Name);
        }
        else
        {
            logger.LogInformation("[Seed]   User skipped  : sysadmin@example.com (already exists)");
        }

        // Accountant — Staff
        if (await userManager.FindByEmailAsync("accountant@example.com") is null)
        {
            var u = MakeUser("accountant@example.com", "Accountant User");
            await userManager.CreateAsync(u, "AccountantPassword123!");
            await userManager.AddToRoleAsync(u, staffRole.Name!);
            logger.LogInformation("[Seed]   User created  : {Email}  role={Role}", u.Email, staffRole.Name);
        }
        else
        {
            logger.LogInformation("[Seed]   User skipped  : accountant@example.com (already exists)");
        }

        // Sales — Staff
        if (await userManager.FindByEmailAsync("sales@example.com") is null)
        {
            var u = MakeUser("sales@example.com", "Sales User");
            await userManager.CreateAsync(u, "SalesPassword123!");
            await userManager.AddToRoleAsync(u, staffRole.Name!);
            logger.LogInformation("[Seed]   User created  : {Email}  role={Role}", u.Email, staffRole.Name);
        }
        else
        {
            logger.LogInformation("[Seed]   User skipped  : sales@example.com (already exists)");
        }

        // Regular user — User
        if (await userManager.FindByEmailAsync("user1@example.com") is null)
        {
            var u = MakeUser("user1@example.com", "Regular User");
            await userManager.CreateAsync(u, "UserPassword123!");
            await userManager.AddToRoleAsync(u, userRole.Name!);
            logger.LogInformation("[Seed]   User created  : {Email}  role={Role}", u.Email, userRole.Name);
        }
        else
        {
            logger.LogInformation("[Seed]   User skipped  : user1@example.com (already exists)");
        }
    }

    /// <summary>
    /// Seeds two demo businesses (ABC Group + XYZ Tech) and wires them to the admin user
    /// so the automatic default-business login flow can be tested out of the box.
    /// Also adds the accountant/sales users to ABC Group as non-default members.
    /// </summary>
    private static async Task SeedBusinessesAndMembershipsAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger logger)
    {
        // Skip if any UserBusiness record already exists
        if (await context.UserBusinesses.AnyAsync())
        {
            var bizCount = await context.Businesses.CountAsync();
            logger.LogInformation("[Seed]   Businesses — skipped ({Count} already in DB).", bizCount);
            return;
        }

        // ── Demo businesses ──────────────────────────────────────────────────
        var abcGroupId = Guid.NewGuid();
        var xyzTechId = Guid.NewGuid();

        if (!await context.Businesses.AnyAsync(b => b.Name == "ABC Group"))
        {
            await context.Businesses.AddRangeAsync(
                new BusinessDomain.Business
                {
                    Id = abcGroupId,
                    Name = "ABC Group",
                    Code = "ABC",
                    ShortName = "ABC",
                    LocalName = "ABC Group",
                    Type = BusinessDomain.BusinessType.Business,
                    VatTin = "ABC-TAX-001",
                    BaseCurrencyCode = "USD",
                    CreatedAtUtc = DateTimeOffset.UtcNow
                },
                new BusinessDomain.Business
                {
                    Id = xyzTechId,
                    Name = "XYZ Tech",
                    Code = "XYZ",
                    ShortName = "XYZ",
                    LocalName = "XYZ Tech",
                    Type = BusinessDomain.BusinessType.Business,
                    VatTin = "XYZ-TAX-001",
                    BaseCurrencyCode = "USD",
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
            await context.SaveChangesAsync();
            logger.LogInformation("[Seed]   Business created: ABC Group  (id={Id})", abcGroupId);
            logger.LogInformation("[Seed]   Business created: XYZ Tech   (id={Id})", xyzTechId);
        }
        else
        {
            abcGroupId = (await context.Businesses.FirstAsync(b => b.Name == "ABC Group")).Id;
            xyzTechId  = (await context.Businesses.FirstAsync(b => b.Name == "XYZ Tech")).Id;
            logger.LogInformation("[Seed]   Business skipped: ABC Group (already exists)");
            logger.LogInformation("[Seed]   Business skipped: XYZ Tech  (already exists)");
        }

        // ── User → Business memberships ──────────────────────────────────────
        var memberships = new List<(string Email, Guid BusinessId, bool IsDefault)>
        {
            // Admin belongs to both: ABC Group is default, XYZ Tech is secondary
            ("admin@example.com",      abcGroupId, true),
            ("admin@example.com",      xyzTechId,  false),
            // Staff users belong to ABC Group only (default)
            ("accountant@example.com", abcGroupId, true),
            ("sales@example.com",      abcGroupId, true),
            // Regular user only belongs to XYZ Tech
            ("user1@example.com",      xyzTechId,  true)
        };

        var userBusinesses = new List<UserBusiness>();
        foreach (var (email, businessId, isDefault) in memberships)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is null) continue;

            userBusinesses.Add(new UserBusiness
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                BusinessId = businessId,
                IsDefault = isDefault,
                IsActive = true,
                JoinedAtUtc = DateTimeOffset.UtcNow
            });
        }

        await context.UserBusinesses.AddRangeAsync(userBusinesses);
        await context.SaveChangesAsync();
        logger.LogInformation("[Seed]   Memberships   : {Count} user-business link(s) created.", userBusinesses.Count);
    }

    /// <summary>
    /// Seeds a BusinessLicense for the admin owner and wires BusinessOwnership
    /// records for every business they own.  Idempotent: skips if already present.
    /// Called from SeedAsync directly so it executes even when UserBusiness rows
    /// already exist (i.e. on subsequent app restarts against an existing database).
    /// </summary>
    private static async Task SeedOwnershipAndLicenseAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger logger)
    {
        // Resolve the two demo businesses by name (they must already exist)
        var abcGroup = await context.Businesses.FirstOrDefaultAsync(b => b.Name == "ABC Group");
        var xyzTech  = await context.Businesses.FirstOrDefaultAsync(b => b.Name == "XYZ Tech");
        if (abcGroup is null || xyzTech is null)
        {
            logger.LogWarning("[Seed]   Ownership & license — demo businesses not found, skipped.");
            return;
        }

        Guid abcGroupId = abcGroup.Id;
        Guid xyzTechId  = xyzTech.Id;
        var admin = await userManager.FindByEmailAsync("admin@example.com");
        if (admin is null)
        {
            logger.LogWarning("[Seed]   Ownership & license — admin@example.com not found, skipped.");
            return;
        }

        // Already seeded?
        if (await context.BusinessOwnerships.AnyAsync(o => o.OwnerId == admin.Id))
        {
            logger.LogInformation("[Seed]   Ownership & license — skipped (already seeded for admin@example.com).");
            return;
        }

        // Create an Enterprise license for the demo owner (no expiry, full features)
        var license = new BusinessDomain.BusinessLicense
        {
            Id = Guid.NewGuid(),
            OwnerId = admin.Id,
            Plan = BusinessDomain.LicensePlan.Enterprise,
            Status = BusinessDomain.LicenseStatus.Active,
            MaxBusinesses = 10,
            MaxBranchesPerBusiness = 20,
            MaxEmployees = 500,
            AllowAdvancedReporting = true,
            AllowMultiCurrency = true,
            AllowIntegrations = true,
            StartsOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ExpiresOn = null,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        await context.BusinessLicenses.AddAsync(license);
        await context.SaveChangesAsync();

        // Create ownership records for both demo businesses
        var ownerships = new[]
        {
            new BusinessDomain.BusinessOwnership
            {
                Id = Guid.NewGuid(),
                OwnerId = admin.Id,
                BusinessId = abcGroupId,
                LicenseId = license.Id,
                IsPrimaryOwner = true,
                GrantedAtUtc = DateTimeOffset.UtcNow,
                RevokedAtUtc = null
            },
            new BusinessDomain.BusinessOwnership
            {
                Id = Guid.NewGuid(),
                OwnerId = admin.Id,
                BusinessId = xyzTechId,
                LicenseId = license.Id,
                IsPrimaryOwner = true,
                GrantedAtUtc = DateTimeOffset.UtcNow,
                RevokedAtUtc = null
            }
        };

        await context.BusinessOwnerships.AddRangeAsync(ownerships);
        await context.SaveChangesAsync();
        logger.LogInformation("[Seed]   License created : Plan={Plan}  OwnerId={OwnerId}  LicenseId={LicenseId}",
            license.Plan, admin.Id, license.Id);
        logger.LogInformation("[Seed]   Ownership wired : admin@example.com → ABC Group, XYZ Tech");
    }

    private static async Task ApplyLicenseMappingsFromFileAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger logger)
    {
        var source = await LoadLicenseMappingJsonAsync(httpClientFactory, configuration, environment, logger);
        if (source is null)
            return;

        var json = source.Value.Json;
        if (string.IsNullOrWhiteSpace(json))
        {
            logger.LogWarning("[Seed] License mapping source is empty ({Source}).", source.Value.Source);
            return;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        jsonOptions.Converters.Add(new JsonStringEnumConverter());

        // Deserialize using the shared GitHubLicenseMappingRoot contract so the
        // local seed file and the remote GitHub file share exactly the same shape.
        var doc = JsonSerializer.Deserialize<GitHubLicenseMappingRoot>(json, jsonOptions);
        if (doc is null || doc.Licenses.Count == 0)
        {
            logger.LogInformation("[Seed] No license mappings found in {Source}.", source.Value.Source);
            return;
        }

        // Validate duplicate emails in seed file (case-insensitive)
        var duplicateEmails = doc.Licenses
            .GroupBy(u => u.Email.Trim().ToLowerInvariant())
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateEmails.Count > 0)
            throw new InvalidOperationException($"Duplicate email(s) in license mapping file: {string.Join(", ", duplicateEmails)}");

        // Validate duplicate license keys in seed file (hashed before storing)
        var duplicateKeys = doc.Licenses
            .Where(u => !string.IsNullOrWhiteSpace(u.License.LicenseKey))
            .Select(u => HashLicenseKey(u.License.LicenseKey))
            .GroupBy(k => k)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateKeys.Count > 0)
            throw new InvalidOperationException("Duplicate license key(s) detected in license mapping file.");

        var defaultPassword = configuration["Seed:LicenseMapping:DefaultPassword"];

        var mappingCreated = 0;
        var mappingUpdated = 0;
        var mappingSkipped = 0;

        foreach (var item in doc.Licenses)
        {
            var email = item.Email.Trim().ToLowerInvariant();
            var user = await userManager.FindByEmailAsync(email);
            var userAction = "existing";

            if (user is null)
            {
                if (string.IsNullOrWhiteSpace(defaultPassword))
                {
                    logger.LogWarning(
                        "[Seed]   Mapping skipped : '{Email}' — user not found and Seed:LicenseMapping:DefaultPassword not set.",
                        email);
                    mappingSkipped++;
                    continue;
                }

                var newUser = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    UserName = email,
                    FullName = string.IsNullOrWhiteSpace(item.FullName) ? email : item.FullName,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                };

                var createResult = await userManager.CreateAsync(newUser, defaultPassword);
                if (!createResult.Succeeded)
                {
                    logger.LogError(
                        "[Seed]   Mapping failed  : '{Email}' — {Errors}",
                        email,
                        string.Join("; ", createResult.Errors.Select(e => e.Description)));
                    mappingSkipped++;
                    continue;
                }

                user = newUser;
                userAction = "created";
            }

            // All entries in the license mapping file are Owners by design.
            // Assign the Owner role if not already held.
            if (!await userManager.IsInRoleAsync(user, SystemRoleNames.Owner))
                await userManager.AddToRoleAsync(user, SystemRoleNames.Owner);

            var hashedLicenseKey = HashLicenseKey(item.License.LicenseKey);

            // Guard: same license key cannot be mapped to multiple users
            var conflictingOwnerId = await context.BusinessLicenses
                .Where(l => l.ExternalSubscriptionId == hashedLicenseKey && l.OwnerId != user.Id)
                .Select(l => (Guid?)l.OwnerId)
                .FirstOrDefaultAsync();

            if (conflictingOwnerId.HasValue)
            {
                logger.LogError(
                    "[Seed] License key conflict for '{Email}'. The key is already assigned to a different owner.",
                    email);
                continue;
            }

            var activeLicense = await context.BusinessLicenses
                .Where(l => l.OwnerId == user.Id && l.Status == BusinessDomain.LicenseStatus.Active)
                .OrderByDescending(l => l.CreatedAtUtc)
                .ToListAsync();

            var license = activeLicense.FirstOrDefault();
            if (license is null)
            {
                license = new BusinessDomain.BusinessLicense
                {
                    Id = Guid.NewGuid(),
                    OwnerId = user.Id,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                };
                await context.BusinessLicenses.AddAsync(license);
            }
            else
            {
                license.ModifiedAtUtc = DateTimeOffset.UtcNow;
            }

            // GitHubLicenseDetail uses string Plan/Status — parse to domain enums.
            if (!Enum.TryParse<BusinessDomain.LicensePlan>(item.License.Plan, ignoreCase: true, out var parsedPlan))
                parsedPlan = BusinessDomain.LicensePlan.Professional;
            if (!Enum.TryParse<BusinessDomain.LicenseStatus>(item.License.Status, ignoreCase: true, out var parsedStatus))
                parsedStatus = BusinessDomain.LicenseStatus.Active;

            var isNewLicense = license.CreatedAtUtc == license.ModifiedAtUtc || license.ModifiedAtUtc is null;

            license.Plan = parsedPlan;
            license.Status = parsedStatus;
            license.MaxBusinesses = item.License.MaxBusinesses;
            license.MaxBranchesPerBusiness = item.License.MaxBranchesPerBusiness;
            license.MaxEmployees = item.License.MaxEmployees;
            license.AllowAdvancedReporting = item.License.AllowAdvancedReporting;
            license.AllowMultiCurrency = item.License.AllowMultiCurrency;
            license.AllowIntegrations = item.License.AllowIntegrations;
            license.StartsOn = item.License.StartsOn;
            license.ExpiresOn = item.License.ExpiresOn;
            // Never persist raw keys; persist deterministic SHA-256 digest only.
            license.ExternalSubscriptionId = hashedLicenseKey;

            await context.SaveChangesAsync();

            if (isNewLicense) mappingCreated++; else mappingUpdated++;

            logger.LogInformation(
                "[Seed]   Mapping {Action}: {Email}  user={UserAction}  plan={Plan}  status={Status}  expires={Expires}  businessIds={BizCount}",
                isNewLicense ? "created" : "updated",
                email,
                userAction,
                parsedPlan,
                parsedStatus,
                item.License.ExpiresOn.HasValue ? item.License.ExpiresOn.Value.ToString("yyyy-MM-dd") : "perpetual",
                item.BusinessIds.Count);

            if (item.BusinessIds.Count == 0) continue;

            // BusinessIds in the mapping are stable GUIDs that match db records
            // (populated by the client after running the business creation wizard).
            var businessGuids = item.BusinessIds
                .Select(id => Guid.TryParse(id, out var g) ? g : (Guid?)null)
                .Where(g => g.HasValue)
                .Select(g => g!.Value)
                .ToList();

            var businesses = await context.Businesses
                .Where(b => businessGuids.Contains(b.Id))
                .ToListAsync();

            var unmatched = businessGuids.Except(businesses.Select(b => b.Id)).ToList();
            if (unmatched.Count > 0)
                logger.LogWarning(
                    "[Seed]   Mapping warning : {Email} — {Count} businessId(s) not found in DB: {Ids}",
                    email, unmatched.Count, string.Join(", ", unmatched));

            var hasDefaultMembership = await context.UserBusinesses
                .AnyAsync(ub => ub.UserId == user.Id && ub.IsDefault);

            var defaultAssigned = hasDefaultMembership;

            foreach (var business in businesses)
            {
                var hasOwnership = await context.BusinessOwnerships
                    .AnyAsync(o => o.OwnerId == user.Id && o.BusinessId == business.Id && o.RevokedAtUtc == null);

                if (!hasOwnership)
                {
                    await context.BusinessOwnerships.AddAsync(new BusinessDomain.BusinessOwnership
                    {
                        Id = Guid.NewGuid(),
                        OwnerId = user.Id,
                        BusinessId = business.Id,
                        LicenseId = license.Id,
                        IsPrimaryOwner = true,
                        GrantedAtUtc = DateTimeOffset.UtcNow
                    });
                }

                var hasMembership = await context.UserBusinesses
                    .AnyAsync(ub => ub.UserId == user.Id && ub.BusinessId == business.Id);

                if (!hasMembership)
                {
                    await context.UserBusinesses.AddAsync(new UserBusiness
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        BusinessId = business.Id,
                        IsDefault = !defaultAssigned,
                        IsActive = true,
                        JoinedAtUtc = DateTimeOffset.UtcNow
                    });

                    if (!defaultAssigned)
                        defaultAssigned = true;
                }
            }

            await context.SaveChangesAsync();
        }

        logger.LogInformation(
            "[Seed]   License mapping complete — created={Created}  updated={Updated}  skipped={Skipped}",
            mappingCreated, mappingUpdated, mappingSkipped);
    }

    private static async Task<(string Json, string Source)?> LoadLicenseMappingJsonAsync(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger logger)
    {
        var sourceUrl = configuration["Seed:LicenseMapping:SourceUrl"];
        var allowLocalFallback = configuration.GetValue("Seed:LicenseMapping:AllowLocalFallback", true);
        var maxBytes = configuration.GetValue("Seed:LicenseMapping:MaxPayloadBytes", 1_048_576); // 1 MB default

        if (!string.IsNullOrWhiteSpace(sourceUrl))
        {
            if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri))
            {
                logger.LogError("[Seed] Seed:LicenseMapping:SourceUrl is invalid: {Url}", sourceUrl);
                if (!allowLocalFallback) return null;
            }
            else if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogError("[Seed] License mapping URL must use HTTPS. Rejected: {Url}", sourceUrl);
                if (!allowLocalFallback) return null;
            }
            else
            {
                try
                {
                    using var http = httpClientFactory.CreateClient("seed-license-mapping");
                    using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);

                    if (!response.IsSuccessStatusCode)
                    {
                        logger.LogError(
                            "[Seed] Failed to fetch license mapping URL {Url}. Status: {StatusCode}",
                            sourceUrl,
                            (int)response.StatusCode);
                        if (!allowLocalFallback) return null;
                    }
                    else
                    {
                        var contentLength = response.Content.Headers.ContentLength;
                        if (contentLength.HasValue && contentLength.Value > maxBytes)
                        {
                            logger.LogError(
                                "[Seed] License mapping payload too large ({Length} bytes > {Max} bytes) from {Url}.",
                                contentLength.Value,
                                maxBytes,
                                sourceUrl);
                            if (!allowLocalFallback) return null;
                        }
                        else
                        {
                            var remoteJson = await response.Content.ReadAsStringAsync();
                            if (!string.IsNullOrWhiteSpace(remoteJson) && Encoding.UTF8.GetByteCount(remoteJson) <= maxBytes)
                            {
                                logger.LogInformation("[Seed] Loaded license mappings from remote URL: {Url}", sourceUrl);
                                return (remoteJson, $"remote URL '{sourceUrl}'");
                            }

                            logger.LogWarning("[Seed] Remote license mapping payload from {Url} is empty or exceeds max bytes.", sourceUrl);
                            if (!allowLocalFallback) return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "[Seed] Error fetching remote license mapping from {Url}.", sourceUrl);
                    if (!allowLocalFallback) return null;
                }
            }
        }

        var configuredPath = configuration["Seed:LicenseMapping:FilePath"];
        var relativeOrAbsolutePath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine("SeedData", "license-mapping.json")
            : configuredPath;

        var fullPath = Path.IsPathRooted(relativeOrAbsolutePath)
            ? relativeOrAbsolutePath
            : Path.Combine(environment.ContentRootPath, relativeOrAbsolutePath);

        if (!File.Exists(fullPath))
        {
            logger.LogInformation("[Seed] License mapping file not found at '{Path}'. Skipping mapping seed.", fullPath);
            return null;
        }

        var localJson = await File.ReadAllTextAsync(fullPath);
        logger.LogInformation("[Seed] Loaded license mappings from local file: {Path}", fullPath);
        return (localJson, $"local file '{fullPath}'");
    }

    private static string HashLicenseKey(string rawLicenseKey)
    {
        if (string.IsNullOrWhiteSpace(rawLicenseKey))
            throw new InvalidOperationException("License key must not be empty in seed mapping.");

        var normalized = rawLicenseKey.Trim();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return $"seed_sha256:{Convert.ToHexString(bytes).ToLowerInvariant()}";
    }

    // NOTE: LicenseMappingSeedDocument, LicenseMappingSeedUser, and
    // LicenseMappingSeedLicense have been removed.  The seed pipeline now
    // deserialises the local file directly into GitHubLicenseMappingRoot
    // (defined in Monolithic.Api.Modules.Business.Contracts) so the local
    // seed file and the remote GitHub mapping share a single schema.
}

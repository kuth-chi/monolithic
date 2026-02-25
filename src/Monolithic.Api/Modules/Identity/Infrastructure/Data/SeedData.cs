using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BusinessDomain = Monolithic.Api.Modules.Business.Domain;
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
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");

        await context.Database.EnsureCreatedAsync();

        // Optional destructive bootstrap reset for local/dev re-install scenarios.
        // Guarded by explicit config + confirmation token to avoid accidental data loss.
        await TryResetBootstrapDataAsync(context, configuration, logger);

        // Seed Permissions
        var permissions = await SeedPermissionsAsync(context);

        // Seed Roles
        var (ownerRole, systemAdminRole, staffRole, userRole) = await SeedRolesAsync(roleManager, context, permissions);

        // Seed Users
        await SeedUsersAsync(userManager, ownerRole, systemAdminRole, staffRole, userRole);

        // Seed demo businesses + user-business memberships (auto-default selection on login)
        await SeedBusinessesAndMembershipsAsync(context, userManager);

        // Seed BusinessLicense + BusinessOwnership for the owner (required by /api/v1/owner/* endpoints).
        // Called unconditionally so it runs even when UserBusiness records already exist.
        await SeedOwnershipAndLicenseAsync(context, userManager);

        // Optional file-driven bootstrap mapping: user -> license (+ optional ownership mappings).
        // Designed for Docker/local first-run scenarios and idempotent re-runs.
        await ApplyLicenseMappingsFromFileAsync(
            context,
            userManager,
            roleManager,
            configuration,
            environment,
            logger);
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

    private static async Task<Dictionary<string, Permission>> SeedPermissionsAsync(ApplicationDbContext context)
    {
        if (context.Permissions.Any())
            return context.Permissions.ToDictionary(p => p.Name);

        var permissions = new List<Permission>
        {
            // Owner permissions (full access)
            new() { Id = Guid.NewGuid(), Name = "*:full", Description = "Full system access" },

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

            // Accounting
            new() { Id = Guid.NewGuid(), Name = "accounting:read", Description = "Read financial/accounting data" },
            new() { Id = Guid.NewGuid(), Name = "accounting:create", Description = "Create accounting entries" },
            new() { Id = Guid.NewGuid(), Name = "accounting:update", Description = "Update accounting entries" },
            new() { Id = Guid.NewGuid(), Name = "accounting:delete", Description = "Delete accounting entries" },

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
            new() { Id = Guid.NewGuid(), Name = "platform:feature-flags:read", Description = "View feature flags" },
            new() { Id = Guid.NewGuid(), Name = "platform:feature-flags:write", Description = "Update and create feature flags" },
            new() { Id = Guid.NewGuid(), Name = "platform:notifications:read",  Description = "View notification logs" },
            new() { Id = Guid.NewGuid(), Name = "platform:notifications:write", Description = "Send notifications" }
        };

        await context.Permissions.AddRangeAsync(permissions);
        await context.SaveChangesAsync();

        return permissions.ToDictionary(p => p.Name);
    }

    private static async Task<(ApplicationRole ownerRole, ApplicationRole systemAdminRole, ApplicationRole staffRole, ApplicationRole userRole)> SeedRolesAsync(
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext context,
        Dictionary<string, Permission> permissions)
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
        }
        else if (!ownerRole.IsSystemRole)
        {
            // Idempotent: mark pre-existing row as system-protected
            ownerRole.IsSystemRole = true;
            await roleManager.UpdateAsync(ownerRole);
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
        }
        else if (!systemAdminRole.IsSystemRole)
        {
            systemAdminRole.IsSystemRole = true;
            await roleManager.UpdateAsync(systemAdminRole);
        }

        // ── Regular roles ─────────────────────────────────────────────────────────────
        ApplicationRole? staffRole = await roleManager.FindByNameAsync("Staff");
        ApplicationRole? userRole = await roleManager.FindByNameAsync("User");

        if (staffRole is null)
        {
            staffRole = new ApplicationRole { Id = Guid.NewGuid(), Name = "Staff", Description = "Staff member with limited access" };
            await roleManager.CreateAsync(staffRole);
        }

        if (userRole is null)
        {
            userRole = new ApplicationRole { Id = Guid.NewGuid(), Name = "User", Description = "Regular user with minimal access" };
            await roleManager.CreateAsync(userRole);
        }

        // Assign permissions to roles — both protected roles get full access
        await AssignPermissionsToRoleAsync(context, ownerRole, permissions, ["*:full"]);
        await AssignPermissionsToRoleAsync(context, systemAdminRole, permissions, ["*:full"]);
        await AssignPermissionsToRoleAsync(context, staffRole, permissions,
        [
            "users:read", "employees:read", "employees:create", "employees:update",
            "sales:read", "sales:create", "sales:update",
            "accounting:read",
            "inventory:read", "inventory:create", "inventory:update",
            "customers:read", "customers:create", "customers:update", "customers:delete",
            "vendors:read", "vendors:create", "vendors:update", "vendors:delete",
            "bankaccounts:read", "bankaccounts:create", "bankaccounts:update", "bankaccounts:delete",
            "purchase:read", "purchase:create", "purchase:update",
            "warehouse:read", "warehouse:create", "warehouse:update",
            "analytics:read", "reports:read"
        ]);
        await AssignPermissionsToRoleAsync(context, userRole, permissions,
        [
            "users:read",
            "sales:read",
            "customers:read",
            "purchase:read",
            "analytics:read", "reports:read"
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
        ApplicationRole userRole)
    {
        // Admin user (Owner)
        if (await userManager.FindByEmailAsync("admin@example.com") is null)
        {
            var adminUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "admin@example.com",
                UserName = "admin@example.com",
                FullName = "Admin User",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            await userManager.CreateAsync(adminUser, "AdminPassword123!");
            await userManager.AddToRoleAsync(adminUser, ownerRole.Name!);
        }

        // System administrator seed user
        if (await userManager.FindByEmailAsync("sysadmin@example.com") is null)
        {
            var sysAdminUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "sysadmin@example.com",
                UserName = "sysadmin@example.com",
                FullName = "System Administrator",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            await userManager.CreateAsync(sysAdminUser, "SysAdminPassword123!");
            await userManager.AddToRoleAsync(sysAdminUser, systemAdminRole.Name!);
        }

        // Accountant user (Staff)
        if (await userManager.FindByEmailAsync("accountant@example.com") is null)
        {
            var accountantUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "accountant@example.com",
                UserName = "accountant@example.com",
                FullName = "Accountant User",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            await userManager.CreateAsync(accountantUser, "AccountantPassword123!");
            await userManager.AddToRoleAsync(accountantUser, staffRole.Name!);
        }

        // Sales user (Staff)
        if (await userManager.FindByEmailAsync("sales@example.com") is null)
        {
            var salesUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "sales@example.com",
                UserName = "sales@example.com",
                FullName = "Sales User",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            await userManager.CreateAsync(salesUser, "SalesPassword123!");
            await userManager.AddToRoleAsync(salesUser, staffRole.Name!);
        }

        // Regular user (User)
        if (await userManager.FindByEmailAsync("user1@example.com") is null)
        {
            var regularUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "user1@example.com",
                UserName = "user1@example.com",
                FullName = "Regular User",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            await userManager.CreateAsync(regularUser, "UserPassword123!");
            await userManager.AddToRoleAsync(regularUser, userRole.Name!);
        }
    }

    /// <summary>
    /// Seeds two demo businesses (ABC Group + XYZ Tech) and wires them to the admin user
    /// so the automatic default-business login flow can be tested out of the box.
    /// Also adds the accountant/sales users to ABC Group as non-default members.
    /// </summary>
    private static async Task SeedBusinessesAndMembershipsAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        // Skip if any UserBusiness record already exists
        if (await context.UserBusinesses.AnyAsync())
            return;

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
        }
        else
        {
            abcGroupId = (await context.Businesses.FirstAsync(b => b.Name == "ABC Group")).Id;
            xyzTechId = (await context.Businesses.FirstAsync(b => b.Name == "XYZ Tech")).Id;
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
    }

    /// <summary>
    /// Seeds a BusinessLicense for the admin owner and wires BusinessOwnership
    /// records for every business they own.  Idempotent: skips if already present.
    /// Called from SeedAsync directly so it executes even when UserBusiness rows
    /// already exist (i.e. on subsequent app restarts against an existing database).
    /// </summary>
    private static async Task SeedOwnershipAndLicenseAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        // Resolve the two demo businesses by name (they must already exist)
        var abcGroup = await context.Businesses.FirstOrDefaultAsync(b => b.Name == "ABC Group");
        var xyzTech = await context.Businesses.FirstOrDefaultAsync(b => b.Name == "XYZ Tech");
        if (abcGroup is null || xyzTech is null) return;  // businesses not seeded yet

        Guid abcGroupId = abcGroup.Id;
        Guid xyzTechId  = xyzTech.Id;
        var admin = await userManager.FindByEmailAsync("admin@example.com");
        if (admin is null) return;

        // Already seeded?
        if (await context.BusinessOwnerships.AnyAsync(o => o.OwnerId == admin.Id))
            return;

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
    }

    private static async Task ApplyLicenseMappingsFromFileAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger logger)
    {
        var configuredPath = configuration["Seed:LicenseMapping:FilePath"];
        var relativeOrAbsolutePath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine("SeedData", "license-mapping.json")
            : configuredPath;

        var fullPath = Path.IsPathRooted(relativeOrAbsolutePath)
            ? relativeOrAbsolutePath
            : Path.Combine(environment.ContentRootPath, relativeOrAbsolutePath);

        if (!File.Exists(fullPath))
        {
            logger.LogInformation("[Seed] License mapping file not found at '{Path}'. Skipping file-based mapping.", fullPath);
            return;
        }

        var json = await File.ReadAllTextAsync(fullPath);
        if (string.IsNullOrWhiteSpace(json))
        {
            logger.LogWarning("[Seed] License mapping file is empty at '{Path}'.", fullPath);
            return;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        jsonOptions.Converters.Add(new JsonStringEnumConverter());

        var doc = JsonSerializer.Deserialize<LicenseMappingSeedDocument>(json, jsonOptions);
        if (doc is null || doc.Users.Count == 0)
        {
            logger.LogInformation("[Seed] No user-license mappings found in '{Path}'.", fullPath);
            return;
        }

        // Validate duplicate emails in seed file (case-insensitive)
        var duplicateEmails = doc.Users
            .GroupBy(u => u.Email.Trim().ToLowerInvariant())
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateEmails.Count > 0)
            throw new InvalidOperationException($"Duplicate email(s) in license mapping file: {string.Join(", ", duplicateEmails)}");

        // Validate duplicate license keys in seed file (hashed before storing)
        var duplicateKeys = doc.Users
            .Where(u => !string.IsNullOrWhiteSpace(u.License.LicenseKey))
            .Select(u => HashLicenseKey(u.License.LicenseKey))
            .GroupBy(k => k)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateKeys.Count > 0)
            throw new InvalidOperationException("Duplicate license key(s) detected in license mapping file.");

        var defaultPassword = configuration["Seed:LicenseMapping:DefaultPassword"];

        foreach (var item in doc.Users)
        {
            var email = item.Email.Trim().ToLowerInvariant();
            var user = await userManager.FindByEmailAsync(email);

            if (user is null)
            {
                if (string.IsNullOrWhiteSpace(defaultPassword))
                {
                    logger.LogWarning(
                        "[Seed] User '{Email}' not found and no Seed:LicenseMapping:DefaultPassword configured. Skipping creation.",
                        email);
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
                        "[Seed] Failed to create user '{Email}': {Errors}",
                        email,
                        string.Join("; ", createResult.Errors.Select(e => e.Description)));
                    continue;
                }

                user = newUser;
            }

            // Role mapping (defaults to Owner if not specified)
            var targetRoles = item.Roles.Count > 0 ? item.Roles : [SystemRoleNames.Owner];
            foreach (var roleName in targetRoles.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var role = await roleManager.FindByNameAsync(roleName);
                if (role is null)
                {
                    logger.LogWarning("[Seed] Role '{Role}' not found for user '{Email}'.", roleName, email);
                    continue;
                }

                if (!await userManager.IsInRoleAsync(user, role.Name!))
                    await userManager.AddToRoleAsync(user, role.Name!);
            }

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

            license.Plan = item.License.Plan;
            license.Status = item.License.Status;
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

            if (item.BusinessNames.Count == 0) continue;

            var businesses = await context.Businesses
                .Where(b => item.BusinessNames.Contains(b.Name))
                .ToListAsync();

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
    }

    private static string HashLicenseKey(string rawLicenseKey)
    {
        if (string.IsNullOrWhiteSpace(rawLicenseKey))
            throw new InvalidOperationException("License key must not be empty in seed mapping.");

        var normalized = rawLicenseKey.Trim();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return $"seed_sha256:{Convert.ToHexString(bytes).ToLowerInvariant()}";
    }

    private sealed record LicenseMappingSeedDocument(IReadOnlyList<LicenseMappingSeedUser> Users)
    {
        public IReadOnlyList<LicenseMappingSeedUser> Users { get; init; } = Users;
    }

    private sealed record LicenseMappingSeedUser(
        string Email,
        string? FullName,
        IReadOnlyList<string> Roles,
        IReadOnlyList<string> BusinessNames,
        LicenseMappingSeedLicense License)
    {
        public string Email { get; init; } = Email;
        public string? FullName { get; init; } = FullName;
        public IReadOnlyList<string> Roles { get; init; } = Roles ?? [];
        public IReadOnlyList<string> BusinessNames { get; init; } = BusinessNames ?? [];
        public LicenseMappingSeedLicense License { get; init; } = License;
    }

    private sealed record LicenseMappingSeedLicense(
        string LicenseKey,
        BusinessDomain.LicensePlan Plan,
        BusinessDomain.LicenseStatus Status,
        int MaxBusinesses,
        int MaxBranchesPerBusiness,
        int MaxEmployees,
        bool AllowAdvancedReporting,
        bool AllowMultiCurrency,
        bool AllowIntegrations,
        DateOnly StartsOn,
        DateOnly? ExpiresOn)
    {
        public string LicenseKey { get; init; } = LicenseKey;
        public BusinessDomain.LicensePlan Plan { get; init; } = Plan;
        public BusinessDomain.LicenseStatus Status { get; init; } = Status;
        public int MaxBusinesses { get; init; } = MaxBusinesses;
        public int MaxBranchesPerBusiness { get; init; } = MaxBranchesPerBusiness;
        public int MaxEmployees { get; init; } = MaxEmployees;
        public bool AllowAdvancedReporting { get; init; } = AllowAdvancedReporting;
        public bool AllowMultiCurrency { get; init; } = AllowMultiCurrency;
        public bool AllowIntegrations { get; init; } = AllowIntegrations;
        public DateOnly StartsOn { get; init; } = StartsOn;
        public DateOnly? ExpiresOn { get; init; } = ExpiresOn;
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

        await context.Database.EnsureCreatedAsync();

        // Seed Permissions
        var permissions = await SeedPermissionsAsync(context);

        // Seed Roles
        var (ownerRole, systemAdminRole, staffRole, userRole) = await SeedRolesAsync(roleManager, context, permissions);

        // Seed Users
        await SeedUsersAsync(userManager, ownerRole, systemAdminRole, staffRole, userRole);

        // Seed demo businesses + user-business memberships (auto-default selection on login)
        await SeedBusinessesAndMembershipsAsync(context, userManager);
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
}

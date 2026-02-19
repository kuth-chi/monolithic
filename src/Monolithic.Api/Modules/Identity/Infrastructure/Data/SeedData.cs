using Microsoft.AspNetCore.Identity;
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
        var (ownerRole, staffRole, userRole) = await SeedRolesAsync(roleManager, context, permissions);

        // Seed Users
        await SeedUsersAsync(userManager, ownerRole, staffRole, userRole);
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
            new() { Id = Guid.NewGuid(), Name = "warehouse:delete", Description = "Delete warehouses and locations" }
        };

        await context.Permissions.AddRangeAsync(permissions);
        await context.SaveChangesAsync();

        return permissions.ToDictionary(p => p.Name);
    }

    private static async Task<(ApplicationRole, ApplicationRole, ApplicationRole)> SeedRolesAsync(
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext context,
        Dictionary<string, Permission> permissions)
    {
        ApplicationRole? ownerRole = await roleManager.FindByNameAsync("Owner");
        ApplicationRole? staffRole = await roleManager.FindByNameAsync("Staff");
        ApplicationRole? userRole = await roleManager.FindByNameAsync("User");

        if (ownerRole is null)
        {
            ownerRole = new ApplicationRole { Id = Guid.NewGuid(), Name = "Owner", Description = "System owner with full access" };
            await roleManager.CreateAsync(ownerRole);
        }

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

        // Assign permissions to roles
        await AssignPermissionsToRoleAsync(context, ownerRole, permissions, ["*:full"]);
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

        return (ownerRole, staffRole, userRole);
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
}

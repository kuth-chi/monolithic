using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Common.Pagination;
using Monolithic.Api.Common.Results;
using Monolithic.Api.Modules.Identity.Domain;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;
using Monolithic.Api.Modules.Users.Contracts;

namespace Monolithic.Api.Modules.Users.Application;

public sealed class RolePermissionService(
    RoleManager<ApplicationRole> roleManager,
    ApplicationDbContext dbContext) : IRolePermissionService
{
    public async Task<PagedResult<RoleSummaryDto>> GetRolesAsync(
        RoleListQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var roleQuery = roleManager.Roles.AsNoTracking();

        if (query.IsSystemRole.HasValue)
            roleQuery = roleQuery.Where(role => role.IsSystemRole == query.IsSystemRole.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var keyword = query.Search.Trim().ToLowerInvariant();
            roleQuery = roleQuery.Where(role =>
                (role.Name ?? string.Empty).ToLower().Contains(keyword) ||
                (role.Description ?? string.Empty).ToLower().Contains(keyword));
        }

        roleQuery = ApplySort(roleQuery, query.SortBy, query.SortDesc);

        var projected = roleQuery.Select(role => new RoleSummaryDto(
            RoleId: role.Id,
            RoleName: role.Name ?? string.Empty,
            RoleDescription: role.Description,
            IsSystemRole: role.IsSystemRole));

        return await projected.ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<Result<RoleEditPermissionsDto>> GetRoleEditPermissionsAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        var role = await roleManager.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

        if (role is null)
            return Error.NotFound("Role.NotFound", $"Role '{roleId}' was not found.");

        return await BuildRoleEditDtoAsync(role, cancellationToken);
    }

    public async Task<Result<RoleEditPermissionsDto>> UpdateRolePermissionsAsync(
        Guid roleId,
        UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var role = await roleManager.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

        if (role is null)
            return Error.NotFound("Role.NotFound", $"Role '{roleId}' was not found.");

        if (role.IsSystemRole)
            return Error.Forbidden("Role.SystemRole.Immutable", "System roles cannot be edited.");

        var requestedIds = request.PermissionIds
            .Distinct()
            .ToHashSet();

        if (requestedIds.Count > 0)
        {
            var validCount = await dbContext.Permissions
                .AsNoTracking()
                .CountAsync(p => requestedIds.Contains(p.Id), cancellationToken);

            if (validCount != requestedIds.Count)
                return Error.Validation("RolePermissions.InvalidPermissionIds", "One or more permission IDs are invalid.");
        }

        var existingRows = await dbContext.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync(cancellationToken);

        var existingIds = existingRows
            .Select(rp => rp.PermissionId)
            .ToHashSet();

        var toRemove = existingRows
            .Where(rp => !requestedIds.Contains(rp.PermissionId))
            .ToList();

        var toAddIds = requestedIds
            .Where(id => !existingIds.Contains(id))
            .ToList();

        if (toRemove.Count > 0)
            dbContext.RolePermissions.RemoveRange(toRemove);

        if (toAddIds.Count > 0)
        {
            var toAdd = toAddIds.Select(permissionId => new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
            });

            await dbContext.RolePermissions.AddRangeAsync(toAdd, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var refreshedRole = await roleManager.Roles
            .AsNoTracking()
            .FirstAsync(r => r.Id == roleId, cancellationToken);

        return await BuildRoleEditDtoAsync(refreshedRole, cancellationToken);
    }

    public async Task<Result<PermissionActionItemDto>> CreatePermissionAsync(
        CreatePermissionRequest request,
        CancellationToken cancellationToken = default)
    {
        var source = NormalizeSegment(request.Source);
        var action = NormalizeSegment(request.Action);
        var featureSegment = NormalizeSegment(request.Feature);
        var permissionName = string.IsNullOrWhiteSpace(request.Permission)
            ? $"{source}:{featureSegment}:{action}"
            : NormalizePermission(request.Permission);

        var exists = await dbContext.Permissions
            .AsNoTracking()
            .AnyAsync(p => p.Name == permissionName, cancellationToken);

        if (exists)
            return Error.Conflict("Permission.Duplicate", $"Permission '{permissionName}' already exists.");

        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            Name = permissionName,
            Source = source,
            GroupName = string.IsNullOrWhiteSpace(request.Group) ? ToTitleCase(source) : request.Group.Trim(),
            FeatureName = request.Feature.Trim(),
            ActionName = action,
            Description = request.Description.Trim(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        await dbContext.Permissions.AddAsync(permission, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new PermissionActionItemDto(
            permission.Id,
            permission.Name,
            permission.Source,
            permission.ActionName,
            permission.Description,
            IsAssigned: false);
    }

    public async Task<Result<RoleSummaryDto>> CreateRoleAsync(
        CreateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("Role.NameRequired", "Role name is required.");

        // Block names that collide with any system-protected constant
        if (SystemRoleNames.IsProtected(name))
            return Error.Forbidden("Role.SystemRole.Reserved",
                $"'{name}' is a reserved system role name and cannot be used.");

        var exists = await roleManager.Roles
            .AnyAsync(r => r.Name == name, cancellationToken);

        if (exists)
            return Error.Conflict("Role.Duplicate", $"A role named '{name}' already exists.");

        var role = new ApplicationRole
        {
            Id          = Guid.NewGuid(),
            Name        = name,
            Description = request.Description.Trim(),
            IsSystemRole = false,   // user-created roles are never system-protected
        };

        var result = await roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Error.Validation("Role.CreateFailed", errors);
        }

        return new RoleSummaryDto(role.Id, role.Name!, role.Description, role.IsSystemRole);
    }

    public async Task<Result> DeleteRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        var role = await roleManager.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

        if (role is null)
            return Error.NotFound("Role.NotFound", $"Role '{roleId}' was not found.");

        // Hard guard — system-seeded roles can never be deleted
        if (role.IsSystemRole || SystemRoleNames.IsProtected(role.Name))
            return Error.Forbidden("Role.SystemRole.Protected",
                $"'{role.Name}' is a system role and cannot be deleted.");

        // Verify no users are currently assigned to this role before removing it
        var assignedUsers = await dbContext.UserRoles
            .AsNoTracking()
            .AnyAsync(ur => ur.RoleId == roleId, cancellationToken);

        if (assignedUsers)
            return Error.Conflict("Role.HasMembers",
                "Cannot delete a role that still has users assigned to it. Remove all users from this role first.");

        // Remove all permission assignments first (FK constraint)
        var rolePerms = await dbContext.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync(cancellationToken);

        if (rolePerms.Count > 0)
            dbContext.RolePermissions.RemoveRange(rolePerms);

        await dbContext.SaveChangesAsync(cancellationToken);

        var deleteResult = await roleManager.DeleteAsync(role);
        if (!deleteResult.Succeeded)
        {
            var errors = string.Join("; ", deleteResult.Errors.Select(e => e.Description));
            return Error.Validation("Role.DeleteFailed", errors);
        }

        return Result.Ok;
    }

    private async Task<RoleEditPermissionsDto> BuildRoleEditDtoAsync(
        ApplicationRole role,
        CancellationToken cancellationToken)
    {
        var assignedIds = await dbContext.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.RoleId == role.Id)
            .Select(rp => rp.PermissionId)
            .ToHashSetAsync(cancellationToken);

        var permissions = await dbContext.Permissions
            .AsNoTracking()
            .OrderBy(p => p.GroupName)
            .ThenBy(p => p.FeatureName)
            .ThenBy(p => p.ActionName)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);

        var groups = permissions
            .GroupBy(p => string.IsNullOrWhiteSpace(p.GroupName) ? "General" : p.GroupName)
            .Select(group => new PermissionGroupDto(
                Group: group.Key,
                Features: group
                    .GroupBy(p => string.IsNullOrWhiteSpace(p.FeatureName) ? "General" : p.FeatureName)
                    .Select(feature => new PermissionFeatureDto(
                        Feature: feature.Key,
                        Actions: feature
                            .Select(permission => new PermissionActionItemDto(
                                PermissionId: permission.Id,
                                Permission: permission.Name,
                                Source: permission.Source,
                                Action: permission.ActionName,
                                Description: permission.Description,
                                IsAssigned: assignedIds.Contains(permission.Id)))
                            .OrderBy(action => action.Action)
                            .ThenBy(action => action.Permission)
                            .ToList()))
                    .OrderBy(feature => feature.Feature)
                    .ToList()))
            .OrderBy(group => group.Group)
            .ToList();

        return new RoleEditPermissionsDto(
            RoleId: role.Id,
            RoleName: role.Name ?? string.Empty,
            RoleDescription: role.Description,
            IsSystemRole: role.IsSystemRole,
            Groups: groups);
    }

    private static string NormalizeSegment(string value)
        => value.Trim().ToLowerInvariant().Replace(' ', '-').Replace('_', '-');

    private static IQueryable<ApplicationRole> ApplySort(
        IQueryable<ApplicationRole> query,
        string? sortBy,
        bool sortDesc)
    {
        var sortField = sortBy?.Trim().ToLowerInvariant();

        return sortField switch
        {
            "name" => sortDesc
                ? query.OrderByDescending(role => role.Name)
                : query.OrderBy(role => role.Name),

            "description" => sortDesc
                ? query.OrderByDescending(role => role.Description)
                : query.OrderBy(role => role.Description),

            "issystemrole" or "is-system-role" => sortDesc
                ? query.OrderByDescending(role => role.IsSystemRole).ThenBy(role => role.Name)
                : query.OrderBy(role => role.IsSystemRole).ThenBy(role => role.Name),

            "createdatutc" or "created-at-utc" or "createdat" => sortDesc
                ? query.OrderByDescending(role => role.CreatedAtUtc)
                : query.OrderBy(role => role.CreatedAtUtc),

            _ => sortDesc
                ? query.OrderByDescending(role => role.Name)
                : query.OrderBy(role => role.Name),
        };
    }

    private static string NormalizePermission(string value)
        => value.Trim().ToLowerInvariant();

    private static string ToTitleCase(string value)
    {
        var cleaned = value
            .Trim()
            .Replace('-', ' ')
            .Replace('_', ' ')
            .ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(cleaned))
            return "General";

        return string.Join(' ', cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(segment => char.ToUpperInvariant(segment[0]) + segment[1..]));
    }
}

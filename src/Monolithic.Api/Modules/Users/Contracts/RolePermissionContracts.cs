using Monolithic.Api.Common.Pagination;

namespace Monolithic.Api.Modules.Users.Contracts;

public sealed record RoleSummaryDto(
    Guid RoleId,
    string RoleName,
    string RoleDescription,
    bool IsSystemRole);

public sealed record RoleEditPermissionsDto(
    Guid RoleId,
    string RoleName,
    string RoleDescription,
    bool IsSystemRole,
    IReadOnlyList<PermissionGroupDto> Groups);

public sealed record PermissionGroupDto(
    string Group,
    IReadOnlyList<PermissionFeatureDto> Features);

public sealed record PermissionFeatureDto(
    string Feature,
    IReadOnlyList<PermissionActionItemDto> Actions);

public sealed record PermissionActionItemDto(
    Guid PermissionId,
    string Permission,
    string Source,
    string Action,
    string Description,
    bool IsAssigned);

public sealed record UpdateRolePermissionsRequest(
    IReadOnlyCollection<Guid> PermissionIds);

public sealed record CreatePermissionRequest(
    string Source,
    string? Group,
    string Feature,
    string Action,
    string? Permission,
    string Description);

public sealed record RoleListQueryParameters : QueryParameters
{
    /// <summary>
    /// Optional filter for system roles.
    /// null = all, true = only system roles, false = only non-system roles.
    /// </summary>
    public bool? IsSystemRole { get; init; }

    public override string ToCacheSegment()
        => $"{base.ToCacheSegment()}:sys{(IsSystemRole.HasValue ? IsSystemRole.Value.ToString().ToLowerInvariant() : "all")}";
}

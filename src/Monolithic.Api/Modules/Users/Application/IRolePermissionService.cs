using Monolithic.Api.Common.Results;
using Monolithic.Api.Common.Pagination;
using Monolithic.Api.Modules.Users.Contracts;

namespace Monolithic.Api.Modules.Users.Application;

public interface IRolePermissionService
{
    Task<PagedResult<RoleSummaryDto>> GetRolesAsync(RoleListQueryParameters query, CancellationToken cancellationToken = default);

    Task<Result<RoleEditPermissionsDto>> GetRoleEditPermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);

    Task<Result<RoleEditPermissionsDto>> UpdateRolePermissionsAsync(Guid roleId, UpdateRolePermissionsRequest request, CancellationToken cancellationToken = default);

    Task<Result<PermissionActionItemDto>> CreatePermissionAsync(CreatePermissionRequest request, CancellationToken cancellationToken = default);
}

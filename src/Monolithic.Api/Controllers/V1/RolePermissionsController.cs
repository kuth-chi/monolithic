using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Common.Extensions;
using Monolithic.Api.Common.Pagination;
using Monolithic.Api.Modules.Identity.Application;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.Identity.Domain;
using Monolithic.Api.Modules.Users.Application;
using Monolithic.Api.Modules.Users.Contracts;

namespace Monolithic.Api.Controllers.V1;

[ApiController]
[Authorize]
[Route("api/v1/users/roles")]
public sealed class RolePermissionsController(
    IRolePermissionService rolePermissionService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("users:roles:read")]
    [ProducesResponseType(typeof(PagedResult<RoleSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRoles(
        [FromQuery] RoleListQueryParameters query,
        CancellationToken cancellationToken)
    {
        if (!IsSystemAdminUser())
            return Forbid();

        var roles = await rolePermissionService.GetRolesAsync(query, cancellationToken);
        return Ok(roles.WithNavigationUrls(Request));
    }

    [HttpGet("{roleId:guid}/edit")]
    [RequirePermission("users:roles:read")]
    [ProducesResponseType(typeof(RoleEditPermissionsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoleForEdit(
        Guid roleId,
        CancellationToken cancellationToken)
    {
        if (!IsSystemAdminUser())
            return Forbid();

        var result = await rolePermissionService.GetRoleEditPermissionsAsync(roleId, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPut("{roleId:guid}/permissions")]
    [RequirePermission("users:roles:admin")]
    [ProducesResponseType(typeof(RoleEditPermissionsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRolePermissions(
        Guid roleId,
        [FromBody] UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsSystemAdminUser())
            return Forbid();

        var result = await rolePermissionService.UpdateRolePermissionsAsync(roleId, request, cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Creates a new non-system role.</summary>
    [HttpPost]
    [RequirePermission("users:roles:admin")]
    [ProducesResponseType(typeof(RoleSummaryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateRole(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsSystemAdminUser())
            return Forbid();

        var result = await rolePermissionService.CreateRoleAsync(request, cancellationToken);
        if (!result.IsSuccess)
            return result.ToActionResult(this);

        var created = result.Value!;
        return CreatedAtAction(nameof(GetRoleForEdit), new { roleId = created.RoleId }, created);
    }

    /// <summary>
    /// Deletes a role permanently.
    /// Returns 403 when the target role is a system-seeded role (Owner, System Admin, Staff, User).
    /// Returns 409 when users are still assigned to the role.
    /// </summary>
    [HttpDelete("{roleId:guid}")]
    [RequirePermission("users:roles:admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteRole(
        Guid roleId,
        CancellationToken cancellationToken)
    {
        if (!IsSystemAdminUser())
            return Forbid();

        var result = await rolePermissionService.DeleteRoleAsync(roleId, cancellationToken);
        return result.ToNoContentResult(this);
    }

    [HttpPost("permissions")]
    [RequirePermission("users:roles:admin")]
    [ProducesResponseType(typeof(PermissionActionItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePermission(
        [FromBody] CreatePermissionRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsSystemAdminUser())
            return Forbid();

        var result = await rolePermissionService.CreatePermissionAsync(request, cancellationToken);
        return result.ToActionResult(this);
    }

    private bool IsSystemAdminUser()
    {
        return User.IsInRole(SystemRoleNames.SystemAdmin)
            || User.IsInRole(SystemRoleNames.Owner)
            || User.HasClaim(AppClaimTypes.Permission, "*:full");
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Common.Extensions;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.Users.Application;
using Monolithic.Api.Modules.Users.Contracts;

namespace Monolithic.Api.Controllers.V1;

/// <summary>
/// User management and profile endpoints.
///
/// Authorization model (layered RBAC + ABAC):
/// ┌────────────────────────────────────────────────────────────────┐
/// │ Resource-based self-data endpoints (ABAC)                      │
/// │  GET  /me          → any authenticated user (self)             │
/// │  PUT  /me          → any authenticated user (self)             │
/// │  GET  /{id}        → self OR users:profiles:read               │
/// │  PUT  /{id}        → self OR users:profiles:write              │
/// ├────────────────────────────────────────────────────────────────┤
/// │ Admin-only endpoints (RBAC)                                    │
/// │  GET  /            → users:profiles:read                       │
/// │  POST /            → users:profiles:write                      │
/// └────────────────────────────────────────────────────────────────┘
/// </summary>
[ApiController]
[Authorize]                          // All endpoints require a valid JWT
[Route("api/v1/users")]
public sealed class UsersController(
    IUserService userService,
    IAuthorizationService authorizationService) : ControllerBase
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── Self-data endpoints (ABAC — no elevated permission required) ──────────

    /// <summary>
    /// Returns the authenticated caller's profile.
    /// Any active user may call this; no elevated permission needed.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var result = await userService.GetProfileAsync(CurrentUserId, cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Updates the authenticated caller's own profile.
    /// Any active user may update their own name/phone.
    /// </summary>
    [HttpPut("me")]
    [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userService.UpdateProfileAsync(CurrentUserId, request, cancellationToken);
        return result.ToActionResult(this);
    }

    // ── Profile by ID (resource-based: self OR elevated permission) ───────────

    /// <summary>
    /// Returns a user's profile.
    /// Succeeds when the caller is the profile owner, holds
    /// <c>users:profiles:read</c>, or holds <c>*:full</c>.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfileById(Guid id, CancellationToken cancellationToken)
    {
        var result = await userService.GetProfileAsync(id, cancellationToken);
        if (!result.IsSuccess) return result.ToActionResult(this);

        // Resource-based authorization: self OR users:profiles:read
        var resource = new OwnedResource<UserProfileDto>(result.Value!, id);
        var authResult = await authorizationService
            .AuthorizeAsync(User, resource, SelfDataPolicies.ProfileReadOrSelf);

        if (!authResult.Succeeded) return Forbid();
        return Ok(result.Value!);
    }

    /// <summary>
    /// Updates a user's profile.
    /// Succeeds when the caller is the profile owner, holds
    /// <c>users:profiles:write</c>, or holds <c>*:full</c>.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfileById(
        Guid id,
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        // First fetch the resource so we know the owner for ABAC check
        var profileResult = await userService.GetProfileAsync(id, cancellationToken);
        if (!profileResult.IsSuccess) return profileResult.ToActionResult(this);

        // Resource-based authorization: self OR users:profiles:write
        var resource = new OwnedResource<UserProfileDto>(profileResult.Value!, id);
        var authResult = await authorizationService
            .AuthorizeAsync(User, resource, SelfDataPolicies.ProfileWriteOrSelf);

        if (!authResult.Succeeded) return Forbid();

        var result = await userService.UpdateProfileAsync(id, request, cancellationToken);
        return result.ToActionResult(this);
    }

    // ── Admin-level list / create ─────────────────────────────────────────────

    /// <summary>
    /// Returns all user profiles.
    /// Requires <c>users:profiles:read</c>.
    /// </summary>
    [HttpGet]
    [RequirePermission("users:profiles:read")]
    [ProducesResponseType<IReadOnlyCollection<UserDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var users = await userService.GetAllAsync(cancellationToken);
        return Ok(users);
    }

    /// <summary>
    /// Creates a new user account (admin workflow).
    /// Requires <c>users:profiles:write</c>.
    /// </summary>
    [HttpPost]
    [RequirePermission("users:profiles:write")]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var createdUser = await userService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetProfileById), new { id = createdUser.Id }, createdUser);
    }
}

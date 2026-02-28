using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Monolithic.Api.Common.Security;
using Monolithic.Api.Modules.Identity.Application;
using Monolithic.Api.Modules.Identity.Contracts;
using System.Security.Claims;

namespace Monolithic.Api.Controllers.V1;

/// <summary>
/// Authentication: login, profile, business-context switching, logout.
/// All data returned by protected endpoints is automatically scoped to the
/// active business embedded in the JWT (via <c>business_id</c> claim).
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IAuthAuditLogger _audit;

    public AuthController(IAuthService auth, IAuthAuditLogger audit)
    {
        _auth = auth;
        _audit = audit;
    }

    // ── POST /api/v1/auth/login ───────────────────────────────────────────────

    /// <summary>
    /// Authenticates a user and returns a JWT scoped to their default business.
    /// The response includes the full user profile, all business memberships,
    /// roles, and effective permissions.
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct = default)
    {
        var result = await _auth.LoginAsync(request, ct);
        return result is null
            ? Unauthorized(new { message = "Invalid credentials or inactive account." })
            : Ok(result);
    }

    // ── POST /api/v1/auth/signup ─────────────────────────────────────────────

    /// <summary>
    /// Registers a new user account and returns a JWT so the client is
    /// immediately authenticated — no second login round-trip required.
    /// Returns 409 Conflict when the email is already in use.
    /// </summary>
    [HttpPost("signup")]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    [ProducesResponseType(typeof(SignUpResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> SignUp(
        [FromBody] SignUpRequest request,
        CancellationToken ct = default)
    {
        var result = await _auth.SignUpAsync(request, ct);
        return result is null
            ? Conflict(new { message = "An account with this email address already exists." })
            : StatusCode(StatusCodes.Status201Created, result);
    }

    // ── GET /api/v1/auth/me ───────────────────────────────────────────────────

    /// <summary>
    /// Returns the current caller's identity, active business context,
    /// all business memberships, roles, and permissions.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var result = await _auth.GetCurrentUserAsync(userId.Value, ct);
        return result is null ? Unauthorized() : Ok(result);
    }

    // ── POST /api/v1/auth/switch-business ─────────────────────────────────────

    /// <summary>
    /// Switches the caller's default business and returns a fresh JWT scoped
    /// to the new context. Only one business per user can have IsDefault = true.
    /// </summary>
    [HttpPost("switch-business")]
    [Authorize]
    [ProducesResponseType(typeof(SwitchBusinessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SwitchBusiness(
        [FromBody] SwitchBusinessRequest request,
        CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var result = await _auth.SwitchDefaultBusinessAsync(userId.Value, request.BusinessId, ct);
        return result is null
            ? BadRequest(new { message = "You do not have an active membership in the requested business." })
            : Ok(result);
    }

    // ── POST /api/v1/auth/logout ──────────────────────────────────────────────

    /// <summary>
    /// Stateless logout — the client should discard the access token.
    /// For token revocation, implement a token-blacklist / refresh-token store.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId is not null)
        {
            var email = User.FindFirstValue(System.Security.Claims.ClaimTypes.Email)
                     ?? User.FindFirstValue("email") ?? string.Empty;
            var businessId = Guid.TryParse(
                User.FindFirstValue(AppClaimTypes.BusinessId), out var bid) ? bid : (Guid?)null;

            await _audit.LogLogoutAsync(userId.Value, email, businessId, ct);
        }
        return Ok(new { message = "Logged out successfully. Please discard your access token." });
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");

        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

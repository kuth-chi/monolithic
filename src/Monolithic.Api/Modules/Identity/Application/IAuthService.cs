using Monolithic.Api.Modules.Identity.Contracts;

namespace Monolithic.Api.Modules.Identity.Application;

/// <summary>
/// Authentication service: registration, login, business-context switching, and caller profile.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Creates a new user account, assigns the default "User" role, and returns a JWT.
    /// Returns <c>null</c> when the email is already registered (caller should return 409).
    /// Throws <see cref="InvalidOperationException"/> when Identity creation fails
    /// (e.g. password policy violations that bypass the validator).
    /// </summary>
    Task<SignUpResponse?> SignUpAsync(SignUpRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates credentials and returns an access token scoped to the user's default business.
    /// Returns <c>null</c> when credentials are invalid or the account is inactive.
    /// </summary>
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the user's default business to <paramref name="targetBusinessId"/>,
    /// enforces the single-default-per-user rule, and issues a fresh access token
    /// scoped to the new context.
    /// Returns <c>null</c> when the user does not have an active membership in the target business.
    /// </summary>
    Task<SwitchBusinessResponse?> SwitchDefaultBusinessAsync(
        Guid userId,
        Guid targetBusinessId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the full caller profile: identity, active-business context, all business memberships,
    /// roles, and effective permissions.
    /// Returns <c>null</c> when the user no longer exists.
    /// </summary>
    Task<MeResponse?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <c>true</c> when at least one <see cref="ApplicationUser"/> row exists in the database.
    /// This anonymous probe lets the frontend redirect a brand-new installation to /signup
    /// instead of /login.  Result is cached (L1 30 s / L2 5 min) to avoid per-request DB probes.
    /// </summary>
    Task<SystemInitResponse> HasAnyUserAsync(CancellationToken cancellationToken = default);
}

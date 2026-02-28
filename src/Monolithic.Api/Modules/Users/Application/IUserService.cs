using Monolithic.Api.Common.Results;
using Monolithic.Api.Modules.Users.Contracts;

namespace Monolithic.Api.Modules.Users.Application;

public interface IUserService
{
    // ── Admin-level list / create ──────────────────────────────────────────
    Task<IReadOnlyCollection<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<int> GetCountAsync(CancellationToken cancellationToken = default);

    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    // ── Profile read / write (self-data + admin-elevated) ──────────────────

    /// <summary>
    /// Returns the full profile for user <paramref name="id"/>.
    /// Authorization is performed by the caller via
    /// <c>SelfDataPolicies.ProfileReadOrSelf</c> before this method is invoked.
    /// </summary>
    Task<Result<UserProfileDto>> GetProfileAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates mutable profile fields for user <paramref name="id"/>.
    /// Authorization is performed by the caller via
    /// <c>SelfDataPolicies.ProfileWriteOrSelf</c> before this method is invoked.
    /// </summary>
    Task<Result<UserProfileDto>> UpdateProfileAsync(Guid id, UpdateProfileRequest request, CancellationToken cancellationToken = default);
}
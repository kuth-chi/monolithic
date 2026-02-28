using Monolithic.Api.Modules.Identity.Domain;

namespace Monolithic.Api.Modules.Identity.Application;

/// <summary>
/// Writes append-only audit records for every authentication event.
/// Captures IP address and User-Agent automatically from the HTTP context.
/// </summary>
public interface IAuthAuditLogger
{
    Task LogSignUpSuccessAsync(Guid userId, string email, CancellationToken ct = default);

    Task LogSignUpFailedAsync(string email, string reason, CancellationToken ct = default);

    Task LogLoginSuccessAsync(Guid userId, string email, Guid? businessId, CancellationToken ct = default);

    Task LogLoginFailedAsync(string email, string reason, CancellationToken ct = default);

    Task LogBusinessSwitchedAsync(Guid userId, string email, Guid previousBusinessId, Guid newBusinessId, CancellationToken ct = default);

    Task LogLogoutAsync(Guid userId, string email, Guid? businessId, CancellationToken ct = default);
}

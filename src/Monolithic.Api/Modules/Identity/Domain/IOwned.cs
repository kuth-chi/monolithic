namespace Monolithic.Api.Modules.Identity.Domain;

/// <summary>
/// Marks a domain entity (or DTO used as an authorization resource) as having
/// a single authoritative owner.
///
/// <para>
/// Used by <c>SelfOwnershipAuthorizationHandler</c> to enforce the
/// <b>self-data rule</b>: a caller may access the resource either because:
/// <list type="bullet">
///   <item>They are the owner (<see cref="OwnerId"/> matches the caller's sub claim), OR</item>
///   <item>They hold an elevated fallback permission (e.g. <c>users:profiles:read</c>), OR</item>
///   <item>They hold the super-admin grant (<c>*:full</c>).</item>
/// </list>
/// </para>
///
/// <para>
/// Only apply this interface to projection objects that are passed to
/// <c>IAuthorizationService.AuthorizeAsync(user, resource, SelfDataPolicies.*)</c>.
/// Do <b>not</b> add it to EF Core entity classes directly — use a thin
/// wrapper record to carry the owner ID alongside the payload.
/// </para>
/// </summary>
public interface IOwned
{
    /// <summary>
    /// The GUID of the user who owns this resource.
    /// Compared against the <c>sub</c> JWT claim (caller's user ID).
    /// </summary>
    Guid OwnerId { get; }
}

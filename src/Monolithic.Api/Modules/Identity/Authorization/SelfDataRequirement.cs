using Microsoft.AspNetCore.Authorization;

namespace Monolithic.Api.Modules.Identity.Authorization;

/// <summary>
/// ASP.NET Core Authorization requirement for the <b>self-data access rule</b>.
///
/// <para>
/// Satisfied when <b>any</b> of the following conditions is true:
/// <list type="number">
///   <item>The caller holds the super-admin grant (<c>*:full</c>).</item>
///   <item>The caller IS the resource owner (<c>sub == OwnerId</c>).</item>
///   <item>The caller holds one of the <see cref="ElevatedPermissions"/> (admin/manager override).</item>
/// </list>
/// </para>
///
/// <para>
/// Register a named policy for each occurrence, e.g.:
/// <code>
///   auth.AddPolicy(SelfDataPolicies.ProfileReadOrSelf, p =>
///       p.AddRequirements(new SelfDataRequirement("users:profiles:read")));
/// </code>
/// </para>
///
/// In controllers, load the resource then call:
/// <code>
///   var authResult = await _authz.AuthorizeAsync(User, ownedResource, SelfDataPolicies.ProfileReadOrSelf);
/// </code>
/// </summary>
public sealed class SelfDataRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// One or more regular (non-self) permissions that are accepted as an
    /// elevated override.  Admins/managers who hold any of these can access
    /// the resource even when they are not the owner.
    /// </summary>
    public IReadOnlyList<string> ElevatedPermissions { get; }

    /// <param name="elevatedPermissions">
    /// The permission keys (e.g. <c>"users:profiles:read"</c>) that grant
    /// elevated access to any owner's data.
    /// At least one must be supplied.
    /// </param>
    public SelfDataRequirement(params string[] elevatedPermissions)
    {
        ArgumentNullException.ThrowIfNull(elevatedPermissions);
        if (elevatedPermissions.Length == 0)
            throw new ArgumentException("At least one elevated permission is required.", nameof(elevatedPermissions));

        ElevatedPermissions = elevatedPermissions.AsReadOnly();
    }
}

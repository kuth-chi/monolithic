namespace Monolithic.Api.Modules.Identity.Application;

/// <summary>
/// Provides the current authenticated user's identity context for
/// <b>service-layer</b> self-data enforcement.
///
/// <para>
/// Inject this into application services to avoid property injection,
/// static access, or passing <c>ClaimsPrincipal</c> into the domain.
/// </para>
///
/// <para>
/// Use-cases:
/// <list type="bullet">
///   <item>Filter queries to only return records owned by the current user.</item>
///   <item>Validate that a write operation targets the current user's data.</item>
///   <item>Decide whether to apply a self-data restriction or allow admin access.</item>
/// </list>
/// </para>
/// </summary>
public interface ISelfDataContext
{
    /// <summary>
    /// Gets the current caller's user ID (the <c>sub</c> JWT claim).
    /// Throws <see cref="InvalidOperationException"/> when called outside an
    /// authenticated HTTP request.
    /// </summary>
    Guid CurrentUserId { get; }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="userId"/> refers to the current caller.
    /// Always returns <c>false</c> for <see cref="Guid.Empty"/>.
    /// </summary>
    bool IsCurrentUser(Guid userId);

    /// <summary>
    /// Returns <c>true</c> when the caller holds the super-admin grant
    /// (<c>*:full</c>) or any of the specified elevated permissions.
    /// </summary>
    bool HasElevatedPermission(params string[] permissions);

    /// <summary>
    /// Returns <c>true</c> when the caller may access data owned by
    /// <paramref name="ownerId"/> — either because they ARE the owner
    /// or because they hold an elevated permission.
    /// </summary>
    bool CanAccessOwnedBy(Guid ownerId, params string[] elevatedPermissions);
}

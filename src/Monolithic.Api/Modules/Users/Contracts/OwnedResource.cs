using Monolithic.Api.Modules.Identity.Domain;

namespace Monolithic.Api.Modules.Users.Contracts;

/// <summary>
/// Thin wrapper that pairs a <typeparamref name="T"/> payload with an owner ID,
/// making any DTO usable as an authorization resource with the
/// <c>SelfOwnershipAuthorizationHandler</c>.
///
/// <para>Usage in controllers:</para>
/// <code>
///   var resource = new OwnedResource&lt;UserProfileDto&gt;(profile, profile.Id);
///   var authResult = await _authorizationService
///       .AuthorizeAsync(User, resource, SelfDataPolicies.ProfileReadOrSelf);
/// </code>
/// </summary>
/// <typeparam name="T">The payload type (any DTO).</typeparam>
public sealed class OwnedResource<T>(T value, Guid ownerId) : IOwned
{
    /// <summary>The wrapped payload.</summary>
    public T Value { get; } = value;

    /// <summary>The user ID that owns this resource.</summary>
    public Guid OwnerId { get; } = ownerId;
}

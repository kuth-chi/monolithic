namespace Monolithic.Api.Modules.Identity.Authorization;

/// <summary>
/// Named authorization policy keys for the self-data rule.
///
/// <para>
/// Each constant maps to an <see cref="SelfDataRequirement"/> registered at startup.
/// Use with:
/// <code>
///   var result = await _authorizationService
///       .AuthorizeAsync(User, ownedResource, SelfDataPolicies.ProfileReadOrSelf);
/// </code>
/// </para>
/// </summary>
public static class SelfDataPolicies
{
    // ── Users / Profiles ──────────────────────────────────────────────────────

    /// <summary>
    /// Policy: caller is the profile owner  OR  holds <c>users:profiles:read</c>.
    /// Used for GET /api/v1/users/{id}.
    /// </summary>
    public const string ProfileReadOrSelf = "SelfData.ProfileReadOrSelf";

    /// <summary>
    /// Policy: caller is the profile owner  OR  holds <c>users:profiles:write</c>.
    /// Used for PUT /api/v1/users/{id}.
    /// </summary>
    public const string ProfileWriteOrSelf = "SelfData.ProfileWriteOrSelf";

    // ── Registers all policies into the IServiceCollection ───────────────────

    /// <summary>
    /// Enumerates all (policy-name → requirement) pairs so they can be
    /// registered in one place (e.g. <c>ModuleRegistry</c> or <c>Program.cs</c>).
    /// </summary>
    public static IEnumerable<(string PolicyName, SelfDataRequirement Requirement)> All()
    {
        yield return (ProfileReadOrSelf,  new SelfDataRequirement("users:profiles:read"));
        yield return (ProfileWriteOrSelf, new SelfDataRequirement("users:profiles:write"));
    }
}

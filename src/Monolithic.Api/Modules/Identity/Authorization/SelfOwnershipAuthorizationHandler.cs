using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Monolithic.Api.Modules.Identity.Application;
using Monolithic.Api.Modules.Identity.Domain;

namespace Monolithic.Api.Modules.Identity.Authorization;

/// <summary>
/// ASP.NET Core resource-based authorization handler that evaluates the
/// <b>self-data rule</b> against an <see cref="IOwned"/> resource.
///
/// <para>
/// Decision matrix:
/// <list type="table">
///   <listheader><term>Condition</term><description>Result</description></listheader>
///   <item><term>Caller holds <c>*:full</c></term>      <description>✅ Succeed (super-admin bypass)</description></item>
///   <item><term><c>sub</c> == resource.OwnerId</term>  <description>✅ Succeed (user owns the resource)</description></item>
///   <item><term>Holds an elevated permission</term>     <description>✅ Succeed (admin override)</description></item>
///   <item><term>None of the above</term>               <description>❌ Do NOT call Fail() — let other handlers run</description></item>
/// </list>
/// </para>
///
/// <para>
/// Registration (in DI):
/// <code>
///   services.AddScoped&lt;IAuthorizationHandler, SelfOwnershipAuthorizationHandler&gt;();
/// </code>
/// </para>
/// </summary>
public sealed class SelfOwnershipAuthorizationHandler
    : AuthorizationHandler<SelfDataRequirement, IOwned>
{
    private const string FullAccessPermission = "*:full";

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SelfDataRequirement requirement,
        IOwned resource)
    {
        // ── Guard 1: super-admin bypass ────────────────────────────────────────
        var hasFullAccess = context.User.HasClaim(
            c => c.Type == AppClaimTypes.Permission && c.Value == FullAccessPermission);

        if (hasFullAccess)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // ── Guard 2: self-ownership check (ABAC) ──────────────────────────────
        var subClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (subClaim is not null
            && Guid.TryParse(subClaim, out var callerId)
            && callerId == resource.OwnerId)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // ── Guard 3: elevated RBAC permission fallback ─────────────────────────
        var hasElevated = requirement.ElevatedPermissions.Any(perm =>
            context.User.HasClaim(c => c.Type == AppClaimTypes.Permission && c.Value == perm));

        if (hasElevated)
        {
            context.Succeed(requirement);
        }

        // Deliberately NOT calling context.Fail() — other handlers may still succeed
        return Task.CompletedTask;
    }
}

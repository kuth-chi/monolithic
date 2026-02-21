using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Monolithic.Api.Modules.Platform.Core.Abstractions;

namespace Monolithic.Api.Modules.Identity.Authorization;

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// Action filter that prevents IDOR attacks on business-scoped endpoints.
///
/// Compares the <c>{businessId}</c> route parameter to the <c>business_id</c>
/// claim embedded in the validated JWT (exposed via <see cref="ITenantContext"/>).
/// If they do not match the request is rejected with <b>403 Forbidden</b>
/// before the action method executes.
///
/// OWASP A01 – Broken Access Control:
///   A user who knows (or guesses) another business's GUID must not be able to
///   access its data simply by substituting it in the URL.
///
/// Usage — decorate any controller or action that exposes <c>{businessId}</c>:
/// <code>
///   [ServiceFilter(typeof(ValidateBusinessAccessFilter))]
/// </code>
/// </summary>
// ─────────────────────────────────────────────────────────────────────────────
public sealed class ValidateBusinessAccessFilter(ITenantContext tenantContext) : IActionFilter
{
    private const string RouteKey          = "businessId";
    private const string FullAccessPermission = "*:full";

    public void OnActionExecuting(ActionExecutingContext context)
    {
        // Super-admin / owner with *:full may cross business boundaries (e.g. support, auditing)
        var hasFullAccess = context.HttpContext.User.HasClaim(c =>
            c.Type == "permission" && c.Value == FullAccessPermission);

        if (hasFullAccess)
            return;

        // Extract the route value (set by ASP.NET Core model binding before this runs)
        if (!context.ActionArguments.TryGetValue(RouteKey, out var raw) || raw is not Guid routeBusinessId)
        {
            // Route param missing or wrong type — let the framework handle it
            return;
        }

        var tokenBusinessId = tenantContext.BusinessId;

        // No business_id claim → the token was issued without business context
        if (tokenBusinessId is null)
        {
            context.Result = new ForbidResult();
            return;
        }

        // Route businessId must match the JWT claim exactly
        if (routeBusinessId != tokenBusinessId.Value)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title  = "Forbidden",
                Detail = "You do not have access to the requested business.",
            })
            {
                StatusCode = StatusCodes.Status403Forbidden,
            };
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { /* no-op */ }
}

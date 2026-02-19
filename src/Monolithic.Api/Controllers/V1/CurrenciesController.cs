using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.Business.Application;
using Monolithic.Api.Modules.Business.Contracts;

namespace Monolithic.Api.Controllers.V1;

/// <summary>
/// Currency management and exchange rate conversion.
/// </summary>
[ApiController]
[Route("api/v1/currencies")]
public sealed class CurrenciesController(ICurrencyService currencyService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await currencyService.GetAllAsync(ct));

    [HttpGet("{code}")]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> GetByCode(string code, CancellationToken ct)
    {
        var result = await currencyService.GetByCodeAsync(code, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{code}")]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> Upsert(string code, [FromBody] UpsertCurrencyRequest request, CancellationToken ct)
    {
        if (!code.Equals(request.Code, StringComparison.OrdinalIgnoreCase))
            return BadRequest("Route code and body code must match.");
        return Ok(await currencyService.UpsertAsync(request, ct));
    }

    [HttpGet("{from}/rates/{to}")]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> GetRates(string from, string to, CancellationToken ct)
        => Ok(await currencyService.GetRatesAsync(from, to, ct));

    [HttpPost("rates")]
    [RequirePermission("finance:write")]
    public async Task<IActionResult> CreateRate([FromBody] CreateExchangeRateRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        return Ok(await currencyService.CreateRateAsync(request, userId, ct));
    }

    [HttpPost("convert")]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> Convert([FromBody] ConvertAmountRequest request, CancellationToken ct)
        => Ok(await currencyService.ConvertAsync(request, ct));

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        return Guid.TryParse(claim?.Value, out var id) ? id : Guid.Empty;
    }
}

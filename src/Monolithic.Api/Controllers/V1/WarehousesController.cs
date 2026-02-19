using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.Inventory.Application;
using Monolithic.Api.Modules.Inventory.Contracts;

namespace Monolithic.Api.Controllers.V1;

[ApiController]
[Route("api/v1/warehouses")]
public sealed class WarehousesController(IWarehouseService warehouseService) : ControllerBase
{
    // ── Warehouses ─────────────────────────────────────────────────────────

    [HttpGet]
    [RequirePermission("warehouse:read")]
    [ProducesResponseType<IReadOnlyCollection<WarehouseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWarehouses(
        [FromQuery] Guid? businessId,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var warehouses = await warehouseService.GetWarehousesAsync(businessId, isActive, cancellationToken);
        return Ok(warehouses);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("warehouse:read")]
    [ProducesResponseType<WarehouseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var warehouse = await warehouseService.GetWarehouseByIdAsync(id, cancellationToken);
        return warehouse is null ? NotFound() : Ok(warehouse);
    }

    [HttpPost]
    [RequirePermission("warehouse:create")]
    [ProducesResponseType<WarehouseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateWarehouseRequest request,
        CancellationToken cancellationToken)
    {
        var created = await warehouseService.CreateWarehouseAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("warehouse:update")]
    [ProducesResponseType<WarehouseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateWarehouseRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await warehouseService.UpdateWarehouseAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("warehouse:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await warehouseService.DeleteWarehouseAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    // ── Warehouse Locations ────────────────────────────────────────────────

    [HttpGet("{warehouseId:guid}/locations")]
    [RequirePermission("warehouse:read")]
    [ProducesResponseType<IReadOnlyCollection<WarehouseLocationDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLocations(
        Guid warehouseId,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var locations = await warehouseService.GetLocationsAsync(warehouseId, isActive, cancellationToken);
        return Ok(locations);
    }

    [HttpGet("locations/{id:guid}")]
    [RequirePermission("warehouse:read")]
    [ProducesResponseType<WarehouseLocationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLocationById(Guid id, CancellationToken cancellationToken)
    {
        var location = await warehouseService.GetLocationByIdAsync(id, cancellationToken);
        return location is null ? NotFound() : Ok(location);
    }

    [HttpPost("locations")]
    [RequirePermission("warehouse:create")]
    [ProducesResponseType<WarehouseLocationDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateLocation(
        [FromBody] CreateWarehouseLocationRequest request,
        CancellationToken cancellationToken)
    {
        var created = await warehouseService.CreateLocationAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetLocationById), new { id = created.Id }, created);
    }

    [HttpPut("locations/{id:guid}")]
    [RequirePermission("warehouse:update")]
    [ProducesResponseType<WarehouseLocationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLocation(
        Guid id,
        [FromBody] UpdateWarehouseLocationRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await warehouseService.UpdateLocationAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("locations/{id:guid}")]
    [RequirePermission("warehouse:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLocation(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await warehouseService.DeleteLocationAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}

using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.Inventory.Application;
using Monolithic.Api.Modules.Inventory.Contracts;
using Monolithic.Api.Modules.Inventory.Domain;

namespace Monolithic.Api.Controllers.V1;

[ApiController]
[Route("api/v1/inventory")]
public sealed class InventoryController(IInventoryService inventoryService) : ControllerBase
{
    // ── Items ──────────────────────────────────────────────────────────────

    [HttpGet("items")]
    [RequirePermission("inventory:read")]
    [ProducesResponseType<IReadOnlyCollection<InventoryItemDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetItems(
        [FromQuery] Guid? businessId,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var items = await inventoryService.GetItemsAsync(businessId, isActive, cancellationToken);
        return Ok(items);
    }

    [HttpGet("items/{id:guid}")]
    [RequirePermission("inventory:read")]
    [ProducesResponseType<InventoryItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetItemById(Guid id, CancellationToken cancellationToken)
    {
        var item = await inventoryService.GetItemByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("items")]
    [RequirePermission("inventory:create")]
    [ProducesResponseType<InventoryItemDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateItem(
        [FromBody] CreateInventoryItemRequest request,
        CancellationToken cancellationToken)
    {
        var created = await inventoryService.CreateItemAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetItemById), new { id = created.Id }, created);
    }

    [HttpPut("items/{id:guid}")]
    [RequirePermission("inventory:update")]
    [ProducesResponseType<InventoryItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateItem(
        Guid id,
        [FromBody] UpdateInventoryItemRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await inventoryService.UpdateItemAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("items/{id:guid}")]
    [RequirePermission("inventory:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteItem(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await inventoryService.DeleteItemAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    // ── Stock ──────────────────────────────────────────────────────────────

    [HttpGet("stock/item/{inventoryItemId:guid}")]
    [RequirePermission("inventory:read")]
    [ProducesResponseType<IReadOnlyCollection<StockDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockByItem(Guid inventoryItemId, CancellationToken cancellationToken)
    {
        var stock = await inventoryService.GetStockByItemAsync(inventoryItemId, cancellationToken);
        return Ok(stock);
    }

    [HttpGet("stock/location/{warehouseLocationId:guid}")]
    [RequirePermission("inventory:read")]
    [ProducesResponseType<IReadOnlyCollection<StockDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockByLocation(Guid warehouseLocationId, CancellationToken cancellationToken)
    {
        var stock = await inventoryService.GetStockByLocationAsync(warehouseLocationId, cancellationToken);
        return Ok(stock);
    }

    [HttpGet("stock/warehouse/{warehouseId:guid}")]
    [RequirePermission("inventory:read")]
    [ProducesResponseType<IReadOnlyCollection<StockDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockByWarehouse(Guid warehouseId, CancellationToken cancellationToken)
    {
        var stock = await inventoryService.GetStockByWarehouseAsync(warehouseId, cancellationToken);
        return Ok(stock);
    }

    // ── Transactions ───────────────────────────────────────────────────────

    [HttpGet("transactions")]
    [RequirePermission("inventory:read")]
    [ProducesResponseType<IReadOnlyCollection<InventoryTransactionDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] Guid? inventoryItemId,
        [FromQuery] Guid? warehouseLocationId,
        [FromQuery] InventoryTransactionType? type,
        CancellationToken cancellationToken)
    {
        var transactions = await inventoryService.GetTransactionsAsync(inventoryItemId, warehouseLocationId, type, cancellationToken);
        return Ok(transactions);
    }

    [HttpPost("transactions")]
    [RequirePermission("inventory:update")]
    [ProducesResponseType<InventoryTransactionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordTransaction(
        [FromBody] CreateInventoryTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        Guid? performedByUserId = userId is not null ? Guid.Parse(userId) : null;

        var transaction = await inventoryService.RecordTransactionAsync(request, performedByUserId, cancellationToken);
        return Created(string.Empty, transaction);
    }

    [HttpGet("items/low-stock")]
    [RequirePermission("inventory:read")]
    [ProducesResponseType<IReadOnlyCollection<InventoryItemDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLowStockItems([FromQuery] Guid businessId, CancellationToken cancellationToken)
    {
        var items = await inventoryService.GetLowStockItemsAsync(businessId, cancellationToken);
        return Ok(items);
    }

    [HttpPost("transactions/transfer")]
    [RequirePermission("inventory:update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TransferStock(
        [FromQuery] Guid inventoryItemId,
        [FromQuery] Guid fromLocationId,
        [FromQuery] Guid toLocationId,
        [FromQuery] decimal quantity,
        [FromQuery] string referenceNumber,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        Guid? performedByUserId = userId is not null ? Guid.Parse(userId) : null;

        await inventoryService.TransferStockAsync(inventoryItemId, fromLocationId, toLocationId, quantity, referenceNumber, performedByUserId, cancellationToken);
        return NoContent();
    }
}

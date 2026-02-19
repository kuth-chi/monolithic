using Monolithic.Api.Modules.Inventory.Contracts;
using Monolithic.Api.Modules.Inventory.Domain;

namespace Monolithic.Api.Modules.Inventory.Application;

public interface IInventoryService
{
    // ── Items ──────────────────────────────────────────────────────────────
    Task<IReadOnlyCollection<InventoryItemDto>> GetItemsAsync(Guid? businessId = null, bool? isActive = null, CancellationToken cancellationToken = default);

    Task<InventoryItemDto?> GetItemByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<InventoryItemDto> CreateItemAsync(CreateInventoryItemRequest request, CancellationToken cancellationToken = default);

    Task<InventoryItemDto?> UpdateItemAsync(Guid id, UpdateInventoryItemRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteItemAsync(Guid id, CancellationToken cancellationToken = default);

    // ── Stock ──────────────────────────────────────────────────────────────
    /// <summary>Returns stock levels for all locations of a given item.</summary>
    Task<IReadOnlyCollection<StockDto>> GetStockByItemAsync(Guid inventoryItemId, CancellationToken cancellationToken = default);

    /// <summary>Returns stock levels for all items at a given warehouse location.</summary>
    Task<IReadOnlyCollection<StockDto>> GetStockByLocationAsync(Guid warehouseLocationId, CancellationToken cancellationToken = default);

    /// <summary>Returns stock levels for all items across an entire warehouse.</summary>
    Task<IReadOnlyCollection<StockDto>> GetStockByWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default);

    // ── Transactions ───────────────────────────────────────────────────────
    Task<IReadOnlyCollection<InventoryTransactionDto>> GetTransactionsAsync(
        Guid? inventoryItemId = null,
        Guid? warehouseLocationId = null,
        InventoryTransactionType? type = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records an inventory movement and updates the corresponding Stock record atomically.
    /// For Transfer transactions, use <see cref="TransferStockAsync"/> instead.
    /// </summary>
    Task<InventoryTransactionDto> RecordTransactionAsync(CreateInventoryTransactionRequest request, Guid? performedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transfers stock from one WarehouseLocation to another.
    /// Creates an Out transaction at the source and an In transaction at the destination.
    /// </summary>
    Task TransferStockAsync(Guid inventoryItemId, Guid fromLocationId, Guid toLocationId, decimal quantity, string referenceNumber, Guid? performedByUserId, CancellationToken cancellationToken = default);
}

using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;
using Monolithic.Api.Modules.Inventory.Contracts;
using Monolithic.Api.Modules.Inventory.Domain;

namespace Monolithic.Api.Modules.Inventory.Application;

public sealed class InventoryService(ApplicationDbContext db) : IInventoryService
{
    // ── Items ──────────────────────────────────────────────────────────────

    public async Task<IReadOnlyCollection<InventoryItemDto>> GetItemsAsync(
        Guid? businessId = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.InventoryItems
            .Include(i => i.Stocks)
            .AsQueryable();

        if (businessId.HasValue)
            query = query.Where(i => i.BusinessId == businessId.Value);

        if (isActive.HasValue)
            query = query.Where(i => i.IsActive == isActive.Value);

        var items = await query.OrderBy(i => i.Name).ToListAsync(cancellationToken);
        return items.Select(MapItemToDto).ToList();
    }

    public async Task<InventoryItemDto?> GetItemByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await db.InventoryItems
            .Include(i => i.Stocks)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        return item is null ? null : MapItemToDto(item);
    }

    public async Task<InventoryItemDto> CreateItemAsync(
        CreateInventoryItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            Sku = request.Sku,
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            Unit = string.IsNullOrWhiteSpace(request.Unit) ? "pieces" : request.Unit,
            ReorderLevel = request.ReorderLevel,
            ReorderQuantity = request.ReorderQuantity,
            CostPrice = request.CostPrice,
            SellingPrice = request.SellingPrice,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        db.InventoryItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return MapItemToDto(item);
    }

    public async Task<InventoryItemDto?> UpdateItemAsync(
        Guid id,
        UpdateInventoryItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var item = await db.InventoryItems
            .Include(i => i.Stocks)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (item is null) return null;

        item.Name = request.Name;
        item.Description = request.Description ?? string.Empty;
        item.Unit = string.IsNullOrWhiteSpace(request.Unit) ? item.Unit : request.Unit;
        item.ReorderLevel = request.ReorderLevel;
        item.ReorderQuantity = request.ReorderQuantity;
        item.CostPrice = request.CostPrice;
        item.SellingPrice = request.SellingPrice;
        item.IsActive = request.IsActive;
        item.ModifiedAtUtc = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return MapItemToDto(item);
    }

    public async Task<bool> DeleteItemAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await db.InventoryItems.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (item is null) return false;

        db.InventoryItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    // ── Stock ──────────────────────────────────────────────────────────────

    public async Task<IReadOnlyCollection<StockDto>> GetStockByItemAsync(
        Guid inventoryItemId,
        CancellationToken cancellationToken = default)
    {
        var stocks = await db.Stocks
            .Include(s => s.InventoryItem)
            .Include(s => s.WarehouseLocation).ThenInclude(wl => wl.Warehouse)
            .Where(s => s.InventoryItemId == inventoryItemId)
            .ToListAsync(cancellationToken);

        return stocks.Select(MapStockToDto).ToList();
    }

    public async Task<IReadOnlyCollection<StockDto>> GetStockByLocationAsync(
        Guid warehouseLocationId,
        CancellationToken cancellationToken = default)
    {
        var stocks = await db.Stocks
            .Include(s => s.InventoryItem)
            .Include(s => s.WarehouseLocation).ThenInclude(wl => wl.Warehouse)
            .Where(s => s.WarehouseLocationId == warehouseLocationId)
            .ToListAsync(cancellationToken);

        return stocks.Select(MapStockToDto).ToList();
    }

    public async Task<IReadOnlyCollection<StockDto>> GetStockByWarehouseAsync(
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        var stocks = await db.Stocks
            .Include(s => s.InventoryItem)
            .Include(s => s.WarehouseLocation).ThenInclude(wl => wl.Warehouse)
            .Where(s => s.WarehouseLocation.WarehouseId == warehouseId)
            .ToListAsync(cancellationToken);

        return stocks.Select(MapStockToDto).ToList();
    }

    // ── Transactions ───────────────────────────────────────────────────────

    public async Task<IReadOnlyCollection<InventoryTransactionDto>> GetTransactionsAsync(
        Guid? inventoryItemId = null,
        Guid? warehouseLocationId = null,
        InventoryTransactionType? type = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.InventoryTransactions
            .Include(t => t.InventoryItem)
            .Include(t => t.WarehouseLocation).ThenInclude(wl => wl.Warehouse)
            .AsQueryable();

        if (inventoryItemId.HasValue)
            query = query.Where(t => t.InventoryItemId == inventoryItemId.Value);

        if (warehouseLocationId.HasValue)
            query = query.Where(t => t.WarehouseLocationId == warehouseLocationId.Value);

        if (type.HasValue)
            query = query.Where(t => t.TransactionType == type.Value);

        var transactions = await query.OrderByDescending(t => t.CreatedAtUtc).ToListAsync(cancellationToken);
        return transactions.Select(MapTransactionToDto).ToList();
    }

    public async Task<InventoryTransactionDto> RecordTransactionAsync(
        CreateInventoryTransactionRequest request,
        Guid? performedByUserId,
        CancellationToken cancellationToken = default)
    {
        // Apply delta to Stock (upsert)
        var stock = await db.Stocks
            .Include(s => s.InventoryItem)
            .Include(s => s.WarehouseLocation).ThenInclude(wl => wl.Warehouse)
            .FirstOrDefaultAsync(s =>
                s.InventoryItemId == request.InventoryItemId &&
                s.WarehouseLocationId == request.WarehouseLocationId,
                cancellationToken);

        if (stock is null)
        {
            stock = new Stock
            {
                Id = Guid.NewGuid(),
                InventoryItemId = request.InventoryItemId,
                WarehouseLocationId = request.WarehouseLocationId,
                QuantityOnHand = 0,
                QuantityReserved = 0,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            db.Stocks.Add(stock);
        }

        var delta = request.TransactionType switch
        {
            InventoryTransactionType.In => request.Quantity,
            InventoryTransactionType.Out => -request.Quantity,
            InventoryTransactionType.Adjustment => request.Quantity, // signed
            _ => throw new InvalidOperationException($"Use TransferStockAsync for Transfer transactions.")
        };

        stock.QuantityOnHand += delta;
        stock.ModifiedAtUtc = DateTimeOffset.UtcNow;

        var transaction = new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            InventoryItemId = request.InventoryItemId,
            WarehouseLocationId = request.WarehouseLocationId,
            TransactionType = request.TransactionType,
            Quantity = request.Quantity,
            ReferenceNumber = request.ReferenceNumber ?? string.Empty,
            Notes = request.Notes ?? string.Empty,
            PerformedByUserId = performedByUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        db.InventoryTransactions.Add(transaction);
        await db.SaveChangesAsync(cancellationToken);

        // Reload navigations for mapping
        await db.Entry(transaction).Reference(t => t.InventoryItem).LoadAsync(cancellationToken);
        await db.Entry(transaction).Reference(t => t.WarehouseLocation).LoadAsync(cancellationToken);
        await db.Entry(transaction.WarehouseLocation).Reference(wl => wl.Warehouse).LoadAsync(cancellationToken);

        return MapTransactionToDto(transaction);
    }

    public async Task TransferStockAsync(
        Guid inventoryItemId,
        Guid fromLocationId,
        Guid toLocationId,
        decimal quantity,
        string referenceNumber,
        Guid? performedByUserId,
        CancellationToken cancellationToken = default)
    {
        await using var txn = await db.Database.BeginTransactionAsync(cancellationToken);

        // Deduct from source
        await ApplyStockDeltaAsync(inventoryItemId, fromLocationId, -quantity, cancellationToken);
        db.InventoryTransactions.Add(new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            InventoryItemId = inventoryItemId,
            WarehouseLocationId = fromLocationId,
            TransactionType = InventoryTransactionType.Transfer,
            Quantity = quantity,
            ReferenceNumber = referenceNumber ?? string.Empty,
            Notes = $"Transfer out to location {toLocationId}",
            PerformedByUserId = performedByUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        // Add to destination
        await ApplyStockDeltaAsync(inventoryItemId, toLocationId, quantity, cancellationToken);
        db.InventoryTransactions.Add(new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            InventoryItemId = inventoryItemId,
            WarehouseLocationId = toLocationId,
            TransactionType = InventoryTransactionType.Transfer,
            Quantity = quantity,
            ReferenceNumber = referenceNumber ?? string.Empty,
            Notes = $"Transfer in from location {fromLocationId}",
            PerformedByUserId = performedByUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        await txn.CommitAsync(cancellationToken);
    }

    // ── Private Helpers ────────────────────────────────────────────────────

    private async Task ApplyStockDeltaAsync(
        Guid inventoryItemId,
        Guid locationId,
        decimal delta,
        CancellationToken cancellationToken)
    {
        var stock = await db.Stocks.FirstOrDefaultAsync(
            s => s.InventoryItemId == inventoryItemId && s.WarehouseLocationId == locationId,
            cancellationToken);

        if (stock is null)
        {
            stock = new Stock
            {
                Id = Guid.NewGuid(),
                InventoryItemId = inventoryItemId,
                WarehouseLocationId = locationId,
                QuantityOnHand = delta,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            db.Stocks.Add(stock);
        }
        else
        {
            stock.QuantityOnHand += delta;
            stock.ModifiedAtUtc = DateTimeOffset.UtcNow;
        }
    }

    private static InventoryItemDto MapItemToDto(InventoryItem i) => new(
        i.Id,
        i.BusinessId,
        i.Sku,
        i.Name,
        i.Description,
        i.Unit,
        i.ReorderLevel,
        i.ReorderQuantity,
        i.CostPrice,
        i.SellingPrice,
        i.IsActive,
        i.Stocks.Sum(s => s.QuantityOnHand),
        i.CreatedAtUtc,
        i.ModifiedAtUtc
    );

    private static StockDto MapStockToDto(Stock s) => new(
        s.Id,
        s.InventoryItemId,
        s.InventoryItem.Sku,
        s.InventoryItem.Name,
        s.WarehouseLocationId,
        s.WarehouseLocation.Code,
        s.WarehouseLocation.Name,
        s.WarehouseLocation.WarehouseId,
        s.WarehouseLocation.Warehouse.Name,
        s.QuantityOnHand,
        s.QuantityReserved,
        s.QuantityAvailable,
        s.CreatedAtUtc,
        s.ModifiedAtUtc
    );

    private static InventoryTransactionDto MapTransactionToDto(InventoryTransaction t) => new(
        t.Id,
        t.InventoryItemId,
        t.InventoryItem.Name,
        t.InventoryItem.Sku,
        t.WarehouseLocationId,
        t.WarehouseLocation.Code,
        t.WarehouseLocation.Warehouse.Name,
        t.TransactionType,
        t.Quantity,
        t.ReferenceNumber,
        t.Notes,
        t.PerformedByUserId,
        t.CreatedAtUtc
    );
}

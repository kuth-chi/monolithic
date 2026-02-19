using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Modules.Business.Domain;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;
using Monolithic.Api.Modules.PurchaseOrders.Contracts;

namespace Monolithic.Api.Modules.PurchaseOrders.Application;

public sealed class PurchaseOrderService(ApplicationDbContext context) : IPurchaseOrderService
{
    public async Task<IReadOnlyCollection<PurchaseOrderDto>> GetAllAsync(Guid? businessId = null, CancellationToken cancellationToken = default)
    {
        var query = context.PurchaseOrders
            .AsNoTracking()
            .Include(po => po.Vendor)
            .Include(po => po.Items)
                .ThenInclude(i => i.InventoryItem)
            .AsQueryable();

        if (businessId.HasValue)
        {
            query = query.Where(po => po.BusinessId == businessId.Value);
        }

        var purchaseOrders = await query
            .OrderByDescending(po => po.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return purchaseOrders.Select(MapToDto).ToList();
    }

    public async Task<PurchaseOrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var po = await context.PurchaseOrders
            .AsNoTracking()
            .Include(p => p.Vendor)
            .Include(p => p.Items)
                .ThenInclude(i => i.InventoryItem)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return po is null ? null : MapToDto(po);
    }

    public async Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderRequest request, Guid? createdByUserId, CancellationToken cancellationToken = default)
    {
        var businessExists = await context.Businesses.AnyAsync(b => b.Id == request.BusinessId, cancellationToken);
        if (!businessExists)
        {
            throw new InvalidOperationException("Business not found.");
        }

        var vendor = await context.Vendors.FirstOrDefaultAsync(v => v.Id == request.VendorId, cancellationToken);
        if (vendor is null)
        {
            throw new InvalidOperationException("Vendor not found.");
        }

        if (vendor.BusinessId != request.BusinessId)
        {
            throw new InvalidOperationException("Vendor does not belong to this business.");
        }

        var requestedItemIds = request.Items.Select(i => i.InventoryItemId).Distinct().ToList();
        var inventoryItems = await context.InventoryItems
            .Where(i => i.BusinessId == request.BusinessId && requestedItemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        if (inventoryItems.Count != requestedItemIds.Count)
        {
            throw new InvalidOperationException("One or more inventory items are invalid for this business.");
        }

        var poNumber = await GeneratePoNumberAsync(request.BusinessId, cancellationToken);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var purchaseOrder = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            VendorId = request.VendorId,
            PoNumber = poNumber,
            Status = PurchaseOrderStatus.Draft,
            OrderDateUtc = DateTimeOffset.UtcNow,
            ExpectedDeliveryDateUtc = request.ExpectedDeliveryDateUtc,
            Notes = request.Notes.Trim(),
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var purchaseOrderItems = new List<PurchaseOrderItem>();

        foreach (var requestedItem in request.Items)
        {
            var inventoryItem = inventoryItems[requestedItem.InventoryItemId];
            var effectiveUnitPrice = requestedItem.UnitPrice.HasValue && requestedItem.UnitPrice.Value > 0
                ? requestedItem.UnitPrice.Value
                : inventoryItem.CostPrice;

            var lineTotal = decimal.Round(requestedItem.Quantity * effectiveUnitPrice, 2, MidpointRounding.AwayFromZero);

            purchaseOrderItems.Add(new PurchaseOrderItem
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = purchaseOrder.Id,
                InventoryItemId = requestedItem.InventoryItemId,
                Quantity = requestedItem.Quantity,
                UnitPrice = effectiveUnitPrice,
                QuantityReceived = 0,
                LineTotal = lineTotal,
                Notes = requestedItem.Notes.Trim(),
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }

        purchaseOrder.TotalAmount = purchaseOrderItems.Sum(i => i.LineTotal);

        await context.PurchaseOrders.AddAsync(purchaseOrder, cancellationToken);
        await context.PurchaseOrderItems.AddRangeAsync(purchaseOrderItems, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        // Reload aggregate for DTO mapping.
        var saved = await context.PurchaseOrders
            .AsNoTracking()
            .Include(p => p.Vendor)
            .Include(p => p.Items)
                .ThenInclude(i => i.InventoryItem)
            .FirstAsync(p => p.Id == purchaseOrder.Id, cancellationToken);

        return MapToDto(saved);
    }

    private async Task<string> GeneratePoNumberAsync(Guid businessId, CancellationToken cancellationToken)
    {
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var prefix = $"PO-{datePart}";

        var existingForToday = await context.PurchaseOrders
            .CountAsync(po => po.BusinessId == businessId && po.PoNumber.StartsWith(prefix), cancellationToken);

        var sequence = existingForToday + 1;
        return $"{prefix}-{sequence:0000}";
    }

    private static PurchaseOrderDto MapToDto(PurchaseOrder purchaseOrder)
    {
        return new PurchaseOrderDto
        {
            Id = purchaseOrder.Id,
            BusinessId = purchaseOrder.BusinessId,
            VendorId = purchaseOrder.VendorId,
            VendorName = purchaseOrder.Vendor?.Name ?? string.Empty,
            PoNumber = purchaseOrder.PoNumber,
            Status = purchaseOrder.Status.ToString(),
            OrderDateUtc = purchaseOrder.OrderDateUtc,
            ExpectedDeliveryDateUtc = purchaseOrder.ExpectedDeliveryDateUtc,
            ReceivedDateUtc = purchaseOrder.ReceivedDateUtc,
            TotalAmount = purchaseOrder.TotalAmount,
            Notes = purchaseOrder.Notes,
            CreatedByUserId = purchaseOrder.CreatedByUserId,
            CreatedAtUtc = purchaseOrder.CreatedAtUtc,
            Items = purchaseOrder.Items.Select(i => new PurchaseOrderItemDto
            {
                Id = i.Id,
                InventoryItemId = i.InventoryItemId,
                InventoryItemName = i.InventoryItem?.Name ?? string.Empty,
                Sku = i.InventoryItem?.Sku ?? string.Empty,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                QuantityReceived = i.QuantityReceived,
                LineTotal = i.LineTotal,
                Notes = i.Notes
            }).ToArray()
        };
    }
}

using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Modules.Business.Contracts;
using Monolithic.Api.Modules.Business.Domain;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;
using Monolithic.Api.Modules.PurchaseOrders.Contracts;

namespace Monolithic.Api.Modules.Business.Application;

public sealed class EstimatePurchaseOrderService(ApplicationDbContext context) : IEstimatePurchaseOrderService
{
    public async Task<EstimatePurchaseOrderDto> CreateAsync(
        CreateEstimatePurchaseOrderRequest request,
        Guid createdByUserId,
        CancellationToken ct = default)
    {
        var rfq = new EstimatePurchaseOrder
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            VendorId = request.VendorId,
            RfqNumber = await GenerateRfqNumberAsync(request.BusinessId, ct),
            Status = EstimatePurchaseOrderStatus.Draft,
            RequestDateUtc = DateTimeOffset.UtcNow,
            CurrencyCode = request.CurrencyCode,
            ExchangeRate = request.ExchangeRate,
            OrderDiscountType = request.OrderDiscountType,
            OrderDiscountValue = request.OrderDiscountValue,
            ShippingFee = request.ShippingFee,
            Notes = request.Notes,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var items = request.Items.Select(i =>
        {
            var lineBeforeDiscount = i.Quantity * i.UnitPrice;
            var discountAmount = i.DiscountType switch
            {
                DiscountType.Amount => i.DiscountValue,
                DiscountType.Percentage => decimal.Round(lineBeforeDiscount * i.DiscountValue / 100, 4),
                _ => 0m
            };
            var lineAfterDiscount = lineBeforeDiscount - discountAmount;
            var taxAmount = decimal.Round(lineAfterDiscount * i.TaxRate, 4);
            return new EstimatePurchaseOrderItem
            {
                Id = Guid.NewGuid(),
                EstimatePurchaseOrderId = rfq.Id,
                InventoryItemId = i.InventoryItemId,
                InventoryItemVariantId = i.InventoryItemVariantId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                DiscountType = i.DiscountType,
                DiscountValue = i.DiscountValue,
                DiscountAmount = discountAmount,
                TaxRate = i.TaxRate,
                TaxAmount = taxAmount,
                LineTotalBeforeDiscount = lineBeforeDiscount,
                LineTotalAfterDiscount = lineAfterDiscount,
                LineTotal = lineAfterDiscount + taxAmount,
                Notes = i.Notes,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
        }).ToList();

        rfq.SubTotal = items.Sum(i => i.LineTotalBeforeDiscount);
        rfq.OrderDiscountAmount = request.OrderDiscountType switch
        {
            DiscountType.Amount => request.OrderDiscountValue,
            DiscountType.Percentage => decimal.Round(rfq.SubTotal * request.OrderDiscountValue / 100, 4),
            _ => 0m
        };
        rfq.TaxAmount = items.Sum(i => i.TaxAmount);
        rfq.TotalAmount = decimal.Round(rfq.SubTotal - rfq.OrderDiscountAmount + rfq.ShippingFee + rfq.TaxAmount, 2);
        rfq.TotalAmountBase = decimal.Round(rfq.TotalAmount * request.ExchangeRate, 2);

        await context.EstimatePurchaseOrders.AddAsync(rfq, ct);
        await context.EstimatePurchaseOrderItems.AddRangeAsync(items, ct);
        await context.SaveChangesAsync(ct);

        return (await GetByIdAsync(rfq.Id, ct))!;
    }

    public async Task<EstimatePurchaseOrderDto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        (await context.EstimatePurchaseOrders.AsNoTracking()
            .Where(e => e.Id == id)
            .Include(e => e.Vendor)
            .Include(e => e.Items).ThenInclude(i => i.InventoryItem)
            .FirstOrDefaultAsync(ct)) is { } rfq ? MapToDto(rfq) : null;

    public async Task<IReadOnlyList<EstimatePurchaseOrderDto>> GetByBusinessAsync(
        Guid businessId, string? statusFilter = null, Guid? vendorId = null, CancellationToken ct = default)
    {
        var q = context.EstimatePurchaseOrders.AsNoTracking()
            .Where(e => e.BusinessId == businessId);
        if (vendorId.HasValue) q = q.Where(e => e.VendorId == vendorId.Value);
        if (!string.IsNullOrWhiteSpace(statusFilter) &&
            Enum.TryParse<EstimatePurchaseOrderStatus>(statusFilter, true, out var s))
            q = q.Where(e => e.Status == s);

        var list = await q.Include(e => e.Vendor)
            .Include(e => e.Items).ThenInclude(i => i.InventoryItem)
            .OrderByDescending(e => e.RequestDateUtc).ToListAsync(ct);
        return list.Select(MapToDto).ToList();
    }

    public async Task SendToVendorAsync(Guid estimateId, CancellationToken ct = default)
    {
        var rfq = await context.EstimatePurchaseOrders.FindAsync([estimateId], ct)
                  ?? throw new KeyNotFoundException($"RFQ {estimateId} not found.");
        if (rfq.Status != EstimatePurchaseOrderStatus.Draft)
            throw new InvalidOperationException("Only Draft RFQs can be sent.");
        rfq.Status = EstimatePurchaseOrderStatus.SentToVendor;
        rfq.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(ct);
    }

    public async Task RecordVendorQuoteAsync(Guid estimateId, DateTimeOffset quoteExpiry, string vendorQuoteRef, CancellationToken ct = default)
    {
        var rfq = await context.EstimatePurchaseOrders.FindAsync([estimateId], ct)
                  ?? throw new KeyNotFoundException($"RFQ {estimateId} not found.");
        rfq.Status = EstimatePurchaseOrderStatus.VendorQuoteReceived;
        rfq.QuoteReceivedDateUtc = DateTimeOffset.UtcNow;
        rfq.QuoteExpiryDateUtc = quoteExpiry;
        rfq.VendorQuoteReference = vendorQuoteRef;
        rfq.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(ct);
    }

    public async Task ApproveAsync(Guid estimateId, Guid approvedByUserId, CancellationToken ct = default)
    {
        var rfq = await context.EstimatePurchaseOrders.FindAsync([estimateId], ct)
                  ?? throw new KeyNotFoundException($"RFQ {estimateId} not found.");
        if (rfq.Status != EstimatePurchaseOrderStatus.VendorQuoteReceived)
            throw new InvalidOperationException("Only RFQs with a received quote can be approved.");
        rfq.Status = EstimatePurchaseOrderStatus.Approved;
        rfq.ApprovedByUserId = approvedByUserId;
        rfq.ApprovedAtUtc = DateTimeOffset.UtcNow;
        rfq.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(ct);
    }

    public async Task<PurchaseOrderDto> ConvertToPurchaseOrderAsync(
        Guid estimateId,
        ConvertEstimateToPurchaseOrderRequest request,
        Guid createdByUserId,
        CancellationToken ct = default)
    {
        var rfq = await context.EstimatePurchaseOrders
            .Include(e => e.Items)
            .FirstOrDefaultAsync(e => e.Id == estimateId, ct)
            ?? throw new KeyNotFoundException($"RFQ {estimateId} not found.");

        if (rfq.Status != EstimatePurchaseOrderStatus.Approved)
            throw new InvalidOperationException("Only Approved RFQs can be converted to a Purchase Order.");

        var exchangeRate = request.ExchangeRateOverride ?? rfq.ExchangeRate;

        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var prefix = $"PO-{datePart}";
        var existing = await context.PurchaseOrders
            .CountAsync(po => po.BusinessId == rfq.BusinessId && po.PoNumber.StartsWith(prefix), ct);
        var poNumber = $"{prefix}-{existing + 1:0000}";

        var po = new Domain.PurchaseOrder
        {
            Id = Guid.NewGuid(),
            BusinessId = rfq.BusinessId,
            VendorId = rfq.VendorId,
            EstimatePurchaseOrderId = rfq.Id,
            PoNumber = poNumber,
            Status = PurchaseOrderStatus.Approved,
            OrderDateUtc = DateTimeOffset.UtcNow,
            CurrencyCode = rfq.CurrencyCode,
            ExchangeRate = exchangeRate,
            OrderDiscountType = rfq.OrderDiscountType,
            OrderDiscountValue = rfq.OrderDiscountValue,
            OrderDiscountAmount = rfq.OrderDiscountAmount,
            ShippingFee = rfq.ShippingFee,
            TaxAmount = rfq.TaxAmount,
            SubTotal = rfq.SubTotal,
            TotalAmount = rfq.TotalAmount,
            TotalAmountBase = decimal.Round(rfq.TotalAmount * exchangeRate, 2),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? rfq.Notes : request.Notes,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var poItems = rfq.Items.Select(i => new Domain.PurchaseOrderItem
        {
            Id = Guid.NewGuid(),
            PurchaseOrderId = po.Id,
            InventoryItemId = i.InventoryItemId,
            InventoryItemVariantId = i.InventoryItemVariantId,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            DiscountType = i.DiscountType,
            DiscountValue = i.DiscountValue,
            DiscountAmount = i.DiscountAmount,
            TaxRate = i.TaxRate,
            TaxAmount = i.TaxAmount,
            LineTotalBeforeDiscount = i.LineTotalBeforeDiscount,
            LineTotalAfterDiscount = i.LineTotalAfterDiscount,
            LineTotal = i.LineTotal,
            Notes = i.Notes,
            CreatedAtUtc = DateTimeOffset.UtcNow
        }).ToList();

        rfq.Status = EstimatePurchaseOrderStatus.ConvertedToPo;
        rfq.ModifiedAtUtc = DateTimeOffset.UtcNow;

        await context.PurchaseOrders.AddAsync(po, ct);
        await context.PurchaseOrderItems.AddRangeAsync(poItems, ct);
        await context.SaveChangesAsync(ct);

        var saved = await context.PurchaseOrders.AsNoTracking()
            .Include(p => p.Vendor)
            .Include(p => p.Items).ThenInclude(i => i.InventoryItem)
            .FirstAsync(p => p.Id == po.Id, ct);

        return new PurchaseOrderDto
        {
            Id = saved.Id,
            BusinessId = saved.BusinessId,
            VendorId = saved.VendorId,
            VendorName = saved.Vendor?.Name ?? string.Empty,
            PoNumber = saved.PoNumber,
            Status = saved.Status.ToString(),
            OrderDateUtc = saved.OrderDateUtc,
            ExpectedDeliveryDateUtc = saved.ExpectedDeliveryDateUtc,
            ReceivedDateUtc = saved.ReceivedDateUtc,
            TotalAmount = saved.TotalAmount,
            Notes = saved.Notes,
            CreatedByUserId = saved.CreatedByUserId,
            CreatedAtUtc = saved.CreatedAtUtc,
            Items = saved.Items.Select(i => new PurchaseOrderItemDto
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

    private async Task<string> GenerateRfqNumberAsync(Guid businessId, CancellationToken ct)
    {
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var prefix = $"RFQ-{datePart}";
        var count = await context.EstimatePurchaseOrders
            .CountAsync(e => e.BusinessId == businessId && e.RfqNumber.StartsWith(prefix), ct);
        return $"{prefix}-{count + 1:0000}";
    }

    private static EstimatePurchaseOrderDto MapToDto(EstimatePurchaseOrder e) =>
        new()
        {
            Id = e.Id,
            BusinessId = e.BusinessId,
            VendorId = e.VendorId,
            VendorName = e.Vendor?.Name ?? string.Empty,
            RfqNumber = e.RfqNumber,
            Status = e.Status.ToString(),
            RequestDateUtc = e.RequestDateUtc,
            QuoteReceivedDateUtc = e.QuoteReceivedDateUtc,
            QuoteExpiryDateUtc = e.QuoteExpiryDateUtc,
            CurrencyCode = e.CurrencyCode,
            ExchangeRate = e.ExchangeRate,
            SubTotal = e.SubTotal,
            OrderDiscountType = e.OrderDiscountType.ToString(),
            OrderDiscountValue = e.OrderDiscountValue,
            OrderDiscountAmount = e.OrderDiscountAmount,
            ShippingFee = e.ShippingFee,
            TaxAmount = e.TaxAmount,
            TotalAmount = e.TotalAmount,
            TotalAmountBase = e.TotalAmountBase,
            Notes = e.Notes,
            VendorQuoteReference = e.VendorQuoteReference,
            CreatedAtUtc = e.CreatedAtUtc,
            Items = e.Items.Select(i => new EstimatePurchaseOrderItemDto
            {
                Id = i.Id,
                InventoryItemId = i.InventoryItemId,
                InventoryItemName = i.InventoryItem?.Name ?? string.Empty,
                Sku = i.InventoryItem?.Sku ?? string.Empty,
                InventoryItemVariantId = i.InventoryItemVariantId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                DiscountType = i.DiscountType.ToString(),
                DiscountValue = i.DiscountValue,
                DiscountAmount = i.DiscountAmount,
                TaxRate = i.TaxRate,
                TaxAmount = i.TaxAmount,
                LineTotal = i.LineTotal,
                Notes = i.Notes
            }).ToList()
        };
}

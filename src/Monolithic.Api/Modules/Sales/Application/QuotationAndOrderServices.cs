using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;
using Monolithic.Api.Modules.Sales.Contracts;
using Monolithic.Api.Modules.Sales.Domain;

namespace Monolithic.Api.Modules.Sales.Application;

// ═══════════════════════════════════════════════════════════════════════════════
// QuotationService
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class QuotationService(ApplicationDbContext db) : IQuotationService
{
    public async Task<IReadOnlyList<QuotationDto>> GetByBusinessAsync(
        Guid businessId, Guid? customerId = null, string? status = null, CancellationToken ct = default)
    {
        var query = db.Quotations.AsNoTracking()
            .Include(q => q.Items)
            .Where(q => q.BusinessId == businessId);

        if (customerId.HasValue) query = query.Where(q => q.CustomerId == customerId.Value);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<QuotationStatus>(status, true, out var s))
            query = query.Where(q => q.Status == s);

        return (await query.OrderByDescending(q => q.QuotationDate).ToListAsync(ct))
            .Select(MapToDto).ToList();
    }

    public async Task<QuotationDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => (await db.Quotations.AsNoTracking()
            .Include(q => q.Items)
            .FirstOrDefaultAsync(q => q.Id == id, ct)) is { } q ? MapToDto(q) : null;

    public async Task<QuotationDto> CreateAsync(CreateQuotationRequest req, Guid createdByUserId, CancellationToken ct = default)
    {
        var quotation = new Quotation
        {
            Id = Guid.NewGuid(),
            BusinessId = req.BusinessId,
            CustomerId = req.CustomerId,
            QuotationNumber = await GenerateNumberAsync(req.BusinessId, ct),
            Status = QuotationStatus.Draft,
            QuotationDate = req.QuotationDate,
            ExpiryDate = req.ExpiryDate,
            CurrencyCode = req.CurrencyCode,
            ExchangeRate = req.ExchangeRate,
            OrderDiscountType = Enum.Parse<SalesDiscountType>(req.OrderDiscountType),
            OrderDiscountValue = req.OrderDiscountValue,
            ShippingFee = req.ShippingFee,
            Notes = req.Notes,
            TermsAndConditions = req.TermsAndConditions,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var items = BuildItems(req.Items, quotation.Id);
        RecalcHeader(quotation, items);

        db.Quotations.Add(quotation);
        db.QuotationItems.AddRange(items);
        await db.SaveChangesAsync(ct);

        quotation.Items = items;
        return MapToDto(quotation);
    }

    public async Task<QuotationDto> UpdateAsync(Guid id, UpdateQuotationRequest req, CancellationToken ct = default)
    {
        var quotation = await db.Quotations.Include(q => q.Items)
            .FirstOrDefaultAsync(q => q.Id == id, ct)
            ?? throw new InvalidOperationException("Quotation not found.");

        if (quotation.Status != QuotationStatus.Draft)
            throw new InvalidOperationException("Only draft quotations can be updated.");

        quotation.ExpiryDate = req.ExpiryDate;
        quotation.CurrencyCode = req.CurrencyCode;
        quotation.ExchangeRate = req.ExchangeRate;
        quotation.OrderDiscountType = Enum.Parse<SalesDiscountType>(req.OrderDiscountType);
        quotation.OrderDiscountValue = req.OrderDiscountValue;
        quotation.ShippingFee = req.ShippingFee;
        quotation.Notes = req.Notes;
        quotation.TermsAndConditions = req.TermsAndConditions;
        quotation.ModifiedAtUtc = DateTimeOffset.UtcNow;

        db.QuotationItems.RemoveRange(quotation.Items);
        var items = BuildItems(req.Items, quotation.Id);
        RecalcHeader(quotation, items);
        quotation.Items = items;
        db.QuotationItems.AddRange(items);

        await db.SaveChangesAsync(ct);
        return MapToDto(quotation);
    }

    public async Task SendAsync(Guid id, CancellationToken ct = default)
    {
        var quotation = await db.Quotations.FirstOrDefaultAsync(q => q.Id == id, ct)
            ?? throw new InvalidOperationException("Quotation not found.");

        if (quotation.Status != QuotationStatus.Draft)
            throw new InvalidOperationException("Only draft quotations can be sent.");

        quotation.Status = QuotationStatus.Sent;
        quotation.SentAtUtc = DateTimeOffset.UtcNow;
        quotation.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<SalesOrderDto> ConvertToOrderAsync(Guid id, ConvertQuotationRequest req, Guid createdByUserId, CancellationToken ct = default)
    {
        var quotation = await db.Quotations.Include(q => q.Items)
            .FirstOrDefaultAsync(q => q.Id == id, ct)
            ?? throw new InvalidOperationException("Quotation not found.");

        if (quotation.Status is not (QuotationStatus.Sent or QuotationStatus.Draft))
            throw new InvalidOperationException("Quotation must be Draft or Sent to convert.");

        var orderSvc = new SalesOrderService(db);
        var createReq = new CreateSalesOrderRequest(
            quotation.BusinessId, quotation.CustomerId,
            req.OrderDate, req.ExpectedDeliveryDate,
            quotation.CurrencyCode, quotation.ExchangeRate,
            quotation.OrderDiscountType.ToString(), quotation.OrderDiscountValue,
            quotation.ShippingFee, req.DeliveryAddress, req.ShippingMethod,
            quotation.Notes, quotation.TermsAndConditions,
            quotation.Items.Select(i => new CreateSalesOrderItemRequest(
                i.InventoryItemId, i.Description, i.Quantity, i.Unit, i.UnitPrice,
                i.DiscountType.ToString(), i.DiscountValue, i.TaxRate, i.Notes, i.SortOrder))
            .ToList());

        var order = await orderSvc.CreateAsync(createReq, createdByUserId, ct);

        quotation.Status = QuotationStatus.Converted;
        quotation.ConvertedToSalesOrderId = order.Id;
        quotation.AcceptedAtUtc = DateTimeOffset.UtcNow;
        quotation.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return order;
    }

    public async Task CancelAsync(Guid id, CancellationToken ct = default)
    {
        var quotation = await db.Quotations.FirstOrDefaultAsync(q => q.Id == id, ct)
            ?? throw new InvalidOperationException("Quotation not found.");

        if (quotation.Status is QuotationStatus.Converted)
            throw new InvalidOperationException("Cannot cancel a converted quotation.");

        quotation.Status = QuotationStatus.Rejected;
        quotation.RejectedAtUtc = DateTimeOffset.UtcNow;
        quotation.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private async Task<string> GenerateNumberAsync(Guid businessId, CancellationToken ct)
    {
        var count = await db.Quotations.CountAsync(q => q.BusinessId == businessId, ct);
        return $"QUO-{DateTime.UtcNow.Year}-{(count + 1):D5}";
    }

    private static List<QuotationItem> BuildItems(IEnumerable<CreateQuotationItemRequest> reqItems, Guid quotationId)
        => reqItems.Select(i =>
        {
            var lineBeforeDiscount = i.Quantity * i.UnitPrice;
            var discType = Enum.Parse<SalesDiscountType>(i.DiscountType);
            var discountAmount = discType switch
            {
                SalesDiscountType.Amount => i.DiscountValue,
                SalesDiscountType.Percentage => decimal.Round(lineBeforeDiscount * i.DiscountValue / 100, 4),
                _ => 0m
            };
            var lineAfterDiscount = lineBeforeDiscount - discountAmount;
            var taxAmount = decimal.Round(lineAfterDiscount * i.TaxRate, 4);
            return new QuotationItem
            {
                Id = Guid.NewGuid(),
                QuotationId = quotationId,
                InventoryItemId = i.InventoryItemId,
                Description = i.Description,
                Quantity = i.Quantity,
                Unit = i.Unit,
                UnitPrice = i.UnitPrice,
                DiscountType = discType,
                DiscountValue = i.DiscountValue,
                DiscountAmount = discountAmount,
                TaxRate = i.TaxRate,
                TaxAmount = taxAmount,
                LineTotalBeforeDiscount = lineBeforeDiscount,
                LineTotalAfterDiscount = lineAfterDiscount,
                LineTotal = lineAfterDiscount + taxAmount,
                Notes = i.Notes,
                SortOrder = i.SortOrder
            };
        }).ToList();

    private static void RecalcHeader(Quotation q, List<QuotationItem> items)
    {
        q.SubTotal = items.Sum(i => i.LineTotalAfterDiscount);
        q.OrderDiscountAmount = q.OrderDiscountType switch
        {
            SalesDiscountType.Amount => q.OrderDiscountValue,
            SalesDiscountType.Percentage => decimal.Round(q.SubTotal * q.OrderDiscountValue / 100, 4),
            _ => 0m
        };
        q.TaxAmount = items.Sum(i => i.TaxAmount);
        q.TotalAmount = decimal.Round(q.SubTotal - q.OrderDiscountAmount + q.ShippingFee + q.TaxAmount, 2);
    }

    internal static QuotationDto MapToDto(Quotation q) => new(
        q.Id, q.BusinessId, q.CustomerId,
        q.QuotationNumber, q.Status.ToString(),
        q.QuotationDate, q.ExpiryDate,
        q.CurrencyCode, q.ExchangeRate,
        q.SubTotal, q.OrderDiscountAmount, q.ShippingFee,
        q.TaxAmount, q.TotalAmount,
        q.Notes, q.TermsAndConditions,
        q.ConvertedToSalesOrderId,
        q.SentAtUtc, q.CreatedAtUtc,
        q.Items.OrderBy(i => i.SortOrder).Select(i => new QuotationItemDto(
            i.Id, i.InventoryItemId, i.Description,
            i.Quantity, i.Unit, i.UnitPrice,
            i.DiscountType.ToString(), i.DiscountValue, i.DiscountAmount,
            i.TaxRate, i.TaxAmount,
            i.LineTotalBeforeDiscount, i.LineTotalAfterDiscount, i.LineTotal,
            i.Notes, i.SortOrder)).ToList());
}

// ═══════════════════════════════════════════════════════════════════════════════
// SalesOrderService
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class SalesOrderService(ApplicationDbContext db) : ISalesOrderService
{
    public async Task<IReadOnlyList<SalesOrderDto>> GetByBusinessAsync(
        Guid businessId, Guid? customerId = null, string? status = null, CancellationToken ct = default)
    {
        var query = db.SalesOrders.AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.BusinessId == businessId);

        if (customerId.HasValue) query = query.Where(o => o.CustomerId == customerId.Value);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SalesOrderStatus>(status, true, out var s))
            query = query.Where(o => o.Status == s);

        return (await query.OrderByDescending(o => o.OrderDate).ToListAsync(ct))
            .Select(MapToDto).ToList();
    }

    public async Task<SalesOrderDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => (await db.SalesOrders.AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct)) is { } o ? MapToDto(o) : null;

    public async Task<SalesOrderDto> CreateAsync(CreateSalesOrderRequest req, Guid createdByUserId, CancellationToken ct = default)
    {
        var order = new SalesOrder
        {
            Id = Guid.NewGuid(),
            BusinessId = req.BusinessId,
            CustomerId = req.CustomerId,
            OrderNumber = await GenerateNumberAsync(req.BusinessId, ct),
            Status = SalesOrderStatus.Draft,
            OrderDate = req.OrderDate,
            ExpectedDeliveryDate = req.ExpectedDeliveryDate,
            CurrencyCode = req.CurrencyCode,
            ExchangeRate = req.ExchangeRate,
            OrderDiscountType = Enum.Parse<SalesDiscountType>(req.OrderDiscountType),
            OrderDiscountValue = req.OrderDiscountValue,
            ShippingFee = req.ShippingFee,
            DeliveryAddress = req.DeliveryAddress,
            ShippingMethod = req.ShippingMethod,
            Notes = req.Notes,
            TermsAndConditions = req.TermsAndConditions,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var items = BuildItems(req.Items, order.Id);
        RecalcHeader(order, items);
        order.TotalAmountBase = decimal.Round(order.TotalAmount * req.ExchangeRate, 2);

        db.SalesOrders.Add(order);
        db.SalesOrderItems.AddRange(items);
        await db.SaveChangesAsync(ct);

        order.Items = items;
        return MapToDto(order);
    }

    public async Task<SalesOrderDto> UpdateAsync(Guid id, UpdateSalesOrderRequest req, CancellationToken ct = default)
    {
        var order = await db.SalesOrders.Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new InvalidOperationException("Sales order not found.");

        if (order.Status is not (SalesOrderStatus.Draft or SalesOrderStatus.Confirmed))
            throw new InvalidOperationException("Cannot update a completed or cancelled order.");

        order.ExpectedDeliveryDate = req.ExpectedDeliveryDate;
        order.DeliveryAddress = req.DeliveryAddress;
        order.ShippingMethod = req.ShippingMethod;
        order.CurrencyCode = req.CurrencyCode;
        order.ExchangeRate = req.ExchangeRate;
        order.OrderDiscountType = Enum.Parse<SalesDiscountType>(req.OrderDiscountType);
        order.OrderDiscountValue = req.OrderDiscountValue;
        order.ShippingFee = req.ShippingFee;
        order.Notes = req.Notes;
        order.TermsAndConditions = req.TermsAndConditions;
        order.ModifiedAtUtc = DateTimeOffset.UtcNow;

        db.SalesOrderItems.RemoveRange(order.Items);
        var items = BuildItems(req.Items, order.Id);
        RecalcHeader(order, items);
        order.TotalAmountBase = decimal.Round(order.TotalAmount * req.ExchangeRate, 2);
        order.Items = items;
        db.SalesOrderItems.AddRange(items);

        await db.SaveChangesAsync(ct);
        return MapToDto(order);
    }

    public async Task ConfirmAsync(Guid id, Guid confirmedByUserId, CancellationToken ct = default)
    {
        var order = await db.SalesOrders.FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new InvalidOperationException("Sales order not found.");

        if (order.Status != SalesOrderStatus.Draft)
            throw new InvalidOperationException("Only draft orders can be confirmed.");

        order.Status = SalesOrderStatus.Confirmed;
        order.ConfirmedByUserId = confirmedByUserId;
        order.ConfirmedAtUtc = DateTimeOffset.UtcNow;
        order.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task CancelAsync(Guid id, CancellationToken ct = default)
    {
        var order = await db.SalesOrders.FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new InvalidOperationException("Sales order not found.");

        if (order.Status is SalesOrderStatus.Completed or SalesOrderStatus.Cancelled)
            throw new InvalidOperationException("Order is already completed or cancelled.");

        order.Status = SalesOrderStatus.Cancelled;
        order.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private async Task<string> GenerateNumberAsync(Guid businessId, CancellationToken ct)
    {
        var count = await db.SalesOrders.CountAsync(o => o.BusinessId == businessId, ct);
        return $"SO-{DateTime.UtcNow.Year}-{(count + 1):D5}";
    }

    private static List<SalesOrderItem> BuildItems(IEnumerable<CreateSalesOrderItemRequest> reqItems, Guid orderId)
        => reqItems.Select(i =>
        {
            var lineBeforeDiscount = i.Quantity * i.UnitPrice;
            var discType = Enum.Parse<SalesDiscountType>(i.DiscountType);
            var discountAmount = discType switch
            {
                SalesDiscountType.Amount => i.DiscountValue,
                SalesDiscountType.Percentage => decimal.Round(lineBeforeDiscount * i.DiscountValue / 100, 4),
                _ => 0m
            };
            var lineAfterDiscount = lineBeforeDiscount - discountAmount;
            var taxAmount = decimal.Round(lineAfterDiscount * i.TaxRate, 4);
            return new SalesOrderItem
            {
                Id = Guid.NewGuid(),
                SalesOrderId = orderId,
                InventoryItemId = i.InventoryItemId,
                Description = i.Description,
                Quantity = i.Quantity,
                Unit = i.Unit,
                UnitPrice = i.UnitPrice,
                DiscountType = discType,
                DiscountValue = i.DiscountValue,
                DiscountAmount = discountAmount,
                TaxRate = i.TaxRate,
                TaxAmount = taxAmount,
                LineTotalBeforeDiscount = lineBeforeDiscount,
                LineTotalAfterDiscount = lineAfterDiscount,
                LineTotal = lineAfterDiscount + taxAmount,
                Notes = i.Notes,
                SortOrder = i.SortOrder
            };
        }).ToList();

    private static void RecalcHeader(SalesOrder o, List<SalesOrderItem> items)
    {
        o.SubTotal = items.Sum(i => i.LineTotalAfterDiscount);
        o.OrderDiscountAmount = o.OrderDiscountType switch
        {
            SalesDiscountType.Amount => o.OrderDiscountValue,
            SalesDiscountType.Percentage => decimal.Round(o.SubTotal * o.OrderDiscountValue / 100, 4),
            _ => 0m
        };
        o.TaxAmount = items.Sum(i => i.TaxAmount);
        o.TotalAmount = decimal.Round(o.SubTotal - o.OrderDiscountAmount + o.ShippingFee + o.TaxAmount, 2);
    }

    internal static SalesOrderDto MapToDto(SalesOrder o) => new(
        o.Id, o.BusinessId, o.CustomerId, o.QuotationId,
        o.OrderNumber, o.Status.ToString(),
        o.OrderDate, o.ExpectedDeliveryDate,
        o.DeliveryAddress, o.ShippingMethod,
        o.CurrencyCode, o.ExchangeRate,
        o.SubTotal, o.OrderDiscountAmount, o.ShippingFee,
        o.TaxAmount, o.TotalAmount, o.TotalAmountBase,
        o.Notes, o.TermsAndConditions,
        o.ConfirmedAtUtc, o.CreatedAtUtc,
        o.Items.OrderBy(i => i.SortOrder).Select(i => new SalesOrderItemDto(
            i.Id, i.InventoryItemId, i.Description,
            i.Quantity, i.QuantityInvoiced,
            i.Unit, i.UnitPrice,
            i.DiscountType.ToString(), i.DiscountValue, i.DiscountAmount,
            i.TaxRate, i.TaxAmount, i.LineTotal,
            i.Notes, i.SortOrder)).ToList());
}

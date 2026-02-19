using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Modules.Business.Contracts;
using Monolithic.Api.Modules.Business.Domain;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;

namespace Monolithic.Api.Modules.Business.Application;

public sealed class VendorBillService(ApplicationDbContext context) : IVendorBillService
{
    // ── Create ────────────────────────────────────────────────────────────────
    public async Task<VendorBillDto> CreateAsync(CreateVendorBillRequest request, Guid createdByUserId, CancellationToken ct = default)
    {
        var bill = new VendorBill
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            VendorId = request.VendorId,
            PurchaseOrderId = request.PurchaseOrderId,
            ChartOfAccountId = request.ChartOfAccountId,
            BillNumber = await GenerateBillNumberAsync(request.BusinessId, ct),
            VendorInvoiceNumber = request.VendorInvoiceNumber,
            Status = VendorBillStatus.Draft,
            BillDate = request.BillDate,
            DueDate = request.DueDate,
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

            return new VendorBillItem
            {
                Id = Guid.NewGuid(),
                VendorBillId = bill.Id,
                PurchaseOrderItemId = i.PurchaseOrderItemId,
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

        // Compute bill totals
        bill.SubTotal = items.Sum(i => i.LineTotalBeforeDiscount);
        bill.OrderDiscountAmount = request.OrderDiscountType switch
        {
            DiscountType.Amount => request.OrderDiscountValue,
            DiscountType.Percentage => decimal.Round(bill.SubTotal * request.OrderDiscountValue / 100, 4),
            _ => 0m
        };
        bill.TaxAmount = items.Sum(i => i.TaxAmount);
        bill.TotalAmount = decimal.Round(
            bill.SubTotal - bill.OrderDiscountAmount + bill.ShippingFee + bill.TaxAmount, 2);
        bill.TotalAmountBase = decimal.Round(bill.TotalAmount * request.ExchangeRate, 2);
        bill.AmountDue = bill.TotalAmount;

        await context.VendorBills.AddAsync(bill, ct);
        await context.VendorBillItems.AddRangeAsync(items, ct);
        await context.SaveChangesAsync(ct);

        return await LoadDtoAsync(bill.Id, ct) ?? throw new InvalidOperationException("Bill not found after save.");
    }

    // ── Query ─────────────────────────────────────────────────────────────────
    public async Task<VendorBillDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await LoadDtoAsync(id, ct);

    public async Task<IReadOnlyList<VendorBillDto>> GetByBusinessAsync(
        Guid businessId,
        string? statusFilter = null,
        Guid? vendorId = null,
        CancellationToken ct = default)
    {
        var query = context.VendorBills
            .AsNoTracking()
            .Where(b => b.BusinessId == businessId);

        if (vendorId.HasValue) query = query.Where(b => b.VendorId == vendorId.Value);

        if (!string.IsNullOrWhiteSpace(statusFilter) &&
            Enum.TryParse<VendorBillStatus>(statusFilter, true, out var parsedStatus))
        {
            query = query.Where(b => b.Status == parsedStatus);
        }

        var bills = await query
            .Include(b => b.Vendor)
            .Include(b => b.PurchaseOrder)
            .Include(b => b.ChartOfAccount)
            .Include(b => b.Items).ThenInclude(i => i.InventoryItem)
            .Include(b => b.Payments)
            .OrderByDescending(b => b.BillDate)
            .ToListAsync(ct);

        return bills.Select(MapToDto).ToList();
    }

    // ── State Changes ─────────────────────────────────────────────────────────
    public async Task ConfirmAsync(Guid billId, Guid confirmedByUserId, CancellationToken ct = default)
    {
        var bill = await context.VendorBills.FirstOrDefaultAsync(b => b.Id == billId, ct)
                   ?? throw new KeyNotFoundException($"VendorBill {billId} not found.");

        if (bill.Status != VendorBillStatus.Draft)
            throw new InvalidOperationException("Only Draft bills can be confirmed.");

        bill.Status = VendorBillStatus.Open;
        bill.ApprovedByUserId = confirmedByUserId;
        bill.ApprovedAtUtc = DateTimeOffset.UtcNow;
        bill.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(ct);
    }

    public async Task CancelAsync(Guid billId, string reason, CancellationToken ct = default)
    {
        var bill = await context.VendorBills.FirstOrDefaultAsync(b => b.Id == billId, ct)
                   ?? throw new KeyNotFoundException($"VendorBill {billId} not found.");

        if (bill.Status is VendorBillStatus.Paid or VendorBillStatus.Cancelled or VendorBillStatus.Voided)
            throw new InvalidOperationException($"Cannot cancel a bill with status {bill.Status}.");

        bill.Status = VendorBillStatus.Cancelled;
        bill.InternalNotes = string.IsNullOrWhiteSpace(reason) ? bill.InternalNotes : $"Cancellation: {reason}";
        bill.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(ct);
    }

    // ── Payments ──────────────────────────────────────────────────────────────
    public async Task<VendorBillPaymentDto> RecordPaymentAsync(
        Guid billId,
        RecordVendorBillPaymentRequest request,
        Guid createdByUserId,
        CancellationToken ct = default)
    {
        var bill = await context.VendorBills.FirstOrDefaultAsync(b => b.Id == billId, ct)
                   ?? throw new KeyNotFoundException($"VendorBill {billId} not found.");

        if (bill.Status is VendorBillStatus.Paid or VendorBillStatus.Cancelled)
            throw new InvalidOperationException($"Cannot record payment on a {bill.Status} bill.");

        if (request.Amount <= 0)
            throw new ArgumentException("Payment amount must be positive.");

        if (request.Amount > bill.AmountDue + 0.01m)
            throw new InvalidOperationException($"Payment ({request.Amount}) exceeds amount due ({bill.AmountDue}).");

        var payment = new VendorBillPayment
        {
            Id = Guid.NewGuid(),
            VendorBillId = billId,
            BankAccountId = request.BankAccountId,
            Amount = request.Amount,
            AmountBase = decimal.Round(request.Amount * request.ExchangeRate, 2),
            CurrencyCode = request.CurrencyCode,
            ExchangeRate = request.ExchangeRate,
            PaymentDate = request.PaymentDate,
            PaymentMethod = request.PaymentMethod,
            Reference = request.Reference,
            Notes = request.Notes,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        bill.AmountPaid = decimal.Round(bill.AmountPaid + request.Amount, 2);
        bill.AmountDue = decimal.Round(bill.TotalAmount - bill.AmountPaid, 2);
        bill.Status = bill.AmountDue <= 0 ? VendorBillStatus.Paid : VendorBillStatus.PartiallyPaid;
        bill.DaysOverdue = 0; // Reset once payment is recorded
        bill.ModifiedAtUtc = DateTimeOffset.UtcNow;

        await context.VendorBillPayments.AddAsync(payment, ct);
        await context.SaveChangesAsync(ct);

        return new VendorBillPaymentDto
        {
            Id = payment.Id,
            Amount = payment.Amount,
            AmountBase = payment.AmountBase,
            CurrencyCode = payment.CurrencyCode,
            ExchangeRate = payment.ExchangeRate,
            PaymentDate = payment.PaymentDate,
            PaymentMethod = payment.PaymentMethod,
            Reference = payment.Reference,
            Notes = payment.Notes,
            CreatedAtUtc = payment.CreatedAtUtc
        };
    }

    // ── Overdue ───────────────────────────────────────────────────────────────
    public async Task<IReadOnlyList<VendorBillDto>> GetOverdueBillsAsync(
        Guid businessId,
        Guid? vendorId = null,
        CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var query = context.VendorBills
            .AsNoTracking()
            .Where(b => b.BusinessId == businessId
                        && b.AmountDue > 0
                        && b.DueDate < today
                        && (b.Status == VendorBillStatus.Open || b.Status == VendorBillStatus.PartiallyPaid || b.Status == VendorBillStatus.Overdue));

        if (vendorId.HasValue) query = query.Where(b => b.VendorId == vendorId.Value);

        var bills = await query
            .Include(b => b.Vendor)
            .Include(b => b.PurchaseOrder)
            .Include(b => b.ChartOfAccount)
            .Include(b => b.Items).ThenInclude(i => i.InventoryItem)
            .Include(b => b.Payments)
            .OrderBy(b => b.DueDate)
            .ToListAsync(ct);

        return bills.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<VendorOverdueSummaryDto>> GetOverdueSummaryByVendorAsync(
        Guid businessId,
        CancellationToken ct = default)
    {
        var business = await context.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == businessId, ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var overdueBills = await context.VendorBills
            .AsNoTracking()
            .Where(b => b.BusinessId == businessId
                        && b.AmountDue > 0
                        && b.DueDate < today
                        && (b.Status == VendorBillStatus.Open || b.Status == VendorBillStatus.PartiallyPaid || b.Status == VendorBillStatus.Overdue))
            .Include(b => b.Vendor)
            .Include(b => b.PurchaseOrder)
            .Include(b => b.ChartOfAccount)
            .Include(b => b.Items).ThenInclude(i => i.InventoryItem)
            .Include(b => b.Payments)
            .ToListAsync(ct);

        var grouped = overdueBills
            .GroupBy(b => b.VendorId)
            .Select(g =>
            {
                var bills = g.Select(MapToDto).ToList();
                var vendor = g.First().Vendor;
                return new VendorOverdueSummaryDto
                {
                    VendorId = g.Key,
                    VendorName = vendor?.Name ?? string.Empty,
                    OverdueBillCount = g.Count(),
                    TotalOverdueAmount = g.Sum(b => b.AmountDue),
                    TotalOverdueAmountBase = g.Sum(b => decimal.Round(b.AmountDue * b.ExchangeRate, 2)),
                    BaseCurrencyCode = business?.BaseCurrencyCode ?? "USD",
                    MaxDaysOverdue = g.Max(b => (today.ToDateTime(TimeOnly.MinValue) - b.DueDate.ToDateTime(TimeOnly.MinValue)).Days),
                    OverdueBills = bills
                };
            })
            .OrderByDescending(s => s.TotalOverdueAmountBase)
            .ToList();

        return grouped;
    }

    public async Task RefreshOverdueStatusAsync(Guid businessId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var openBills = await context.VendorBills
            .Where(b => b.BusinessId == businessId
                        && b.AmountDue > 0
                        && (b.Status == VendorBillStatus.Open || b.Status == VendorBillStatus.PartiallyPaid || b.Status == VendorBillStatus.Overdue))
            .ToListAsync(ct);

        foreach (var bill in openBills)
        {
            var days = (today.ToDateTime(TimeOnly.MinValue) - bill.DueDate.ToDateTime(TimeOnly.MinValue)).Days;
            bill.DaysOverdue = Math.Max(0, days);
            if (days > 0 && bill.Status != VendorBillStatus.Overdue)
                bill.Status = VendorBillStatus.Overdue;

            bill.ModifiedAtUtc = DateTimeOffset.UtcNow;
        }

        await context.SaveChangesAsync(ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private async Task<string> GenerateBillNumberAsync(Guid businessId, CancellationToken ct)
    {
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var prefix = $"BILL-{datePart}";
        var count = await context.VendorBills
            .CountAsync(b => b.BusinessId == businessId && b.BillNumber.StartsWith(prefix), ct);
        return $"{prefix}-{count + 1:0000}";
    }

    private async Task<VendorBillDto?> LoadDtoAsync(Guid id, CancellationToken ct) =>
        (await context.VendorBills
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Include(b => b.Vendor)
            .Include(b => b.PurchaseOrder)
            .Include(b => b.ChartOfAccount)
            .Include(b => b.Items).ThenInclude(i => i.InventoryItem)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(ct)) is { } bill ? MapToDto(bill) : null;

    private static VendorBillDto MapToDto(VendorBill b)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var daysOverdue = b.AmountDue > 0 && b.DueDate < today
            ? (today.ToDateTime(TimeOnly.MinValue) - b.DueDate.ToDateTime(TimeOnly.MinValue)).Days
            : 0;

        return new VendorBillDto
        {
            Id = b.Id,
            BusinessId = b.BusinessId,
            VendorId = b.VendorId,
            VendorName = b.Vendor?.Name ?? string.Empty,
            PurchaseOrderId = b.PurchaseOrderId,
            PoNumber = b.PurchaseOrder?.PoNumber,
            ChartOfAccountId = b.ChartOfAccountId,
            ChartOfAccountName = b.ChartOfAccount?.Name,
            BillNumber = b.BillNumber,
            VendorInvoiceNumber = b.VendorInvoiceNumber,
            Status = b.Status.ToString(),
            BillDate = b.BillDate,
            DueDate = b.DueDate,
            CurrencyCode = b.CurrencyCode,
            ExchangeRate = b.ExchangeRate,
            SubTotal = b.SubTotal,
            OrderDiscountType = b.OrderDiscountType.ToString(),
            OrderDiscountValue = b.OrderDiscountValue,
            OrderDiscountAmount = b.OrderDiscountAmount,
            ShippingFee = b.ShippingFee,
            TaxAmount = b.TaxAmount,
            TotalAmount = b.TotalAmount,
            TotalAmountBase = b.TotalAmountBase,
            AmountPaid = b.AmountPaid,
            AmountDue = b.AmountDue,
            DaysOverdue = daysOverdue,
            IsOverdue = daysOverdue > 0,
            Notes = b.Notes,
            CreatedAtUtc = b.CreatedAtUtc,
            Items = b.Items.Select(i => new VendorBillItemDto
            {
                Id = i.Id,
                PurchaseOrderItemId = i.PurchaseOrderItemId,
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
                LineTotalBeforeDiscount = i.LineTotalBeforeDiscount,
                LineTotalAfterDiscount = i.LineTotalAfterDiscount,
                LineTotal = i.LineTotal,
                Notes = i.Notes
            }).ToList(),
            Payments = b.Payments.Select(p => new VendorBillPaymentDto
            {
                Id = p.Id,
                Amount = p.Amount,
                AmountBase = p.AmountBase,
                CurrencyCode = p.CurrencyCode,
                ExchangeRate = p.ExchangeRate,
                PaymentDate = p.PaymentDate,
                PaymentMethod = p.PaymentMethod,
                Reference = p.Reference,
                Notes = p.Notes,
                CreatedAtUtc = p.CreatedAtUtc
            }).ToList()
        };
    }
}

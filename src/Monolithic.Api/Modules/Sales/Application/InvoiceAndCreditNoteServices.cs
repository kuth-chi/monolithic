using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;
using Monolithic.Api.Modules.Sales.Contracts;
using Monolithic.Api.Modules.Sales.Domain;

namespace Monolithic.Api.Modules.Sales.Application;

// ═══════════════════════════════════════════════════════════════════════════════
// SalesInvoiceService
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class SalesInvoiceService(ApplicationDbContext db) : ISalesInvoiceService
{
    public async Task<IReadOnlyList<SalesInvoiceDto>> GetByBusinessAsync(
        Guid businessId, Guid? customerId = null, string? status = null, CancellationToken ct = default)
    {
        var query = db.SalesInvoices.AsNoTracking()
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .Where(i => i.BusinessId == businessId);

        if (customerId.HasValue) query = query.Where(i => i.CustomerId == customerId.Value);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SalesInvoiceStatus>(status, true, out var s))
            query = query.Where(i => i.Status == s);

        return (await query.OrderByDescending(i => i.InvoiceDate).ToListAsync(ct))
            .Select(MapToDto).ToList();
    }

    public async Task<SalesInvoiceDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await LoadDtoAsync(id, ct);

    public async Task<IReadOnlyList<SalesInvoiceDto>> GetOverdueAsync(Guid businessId, Guid? customerId = null, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var query = db.SalesInvoices.AsNoTracking()
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .Where(i => i.BusinessId == businessId
                        && i.AmountDue > 0
                        && i.DueDate < today
                        && (i.Status == SalesInvoiceStatus.Sent
                            || i.Status == SalesInvoiceStatus.PartiallyPaid
                            || i.Status == SalesInvoiceStatus.Overdue));

        if (customerId.HasValue) query = query.Where(i => i.CustomerId == customerId.Value);
        return (await query.OrderBy(i => i.DueDate).ToListAsync(ct)).Select(MapToDto).ToList();
    }

    public async Task<ArDashboardDto> GetDashboardAsync(Guid businessId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var invoices = await db.SalesInvoices
            .AsNoTracking()
            .Where(i => i.BusinessId == businessId
                        && i.AmountDue > 0
                        && i.Status != SalesInvoiceStatus.Void)
            .Select(i => new { i.CustomerId, i.AmountDue, i.DueDate, IsOverdue = i.DueDate < today })
            .ToListAsync(ct);

        var totalOutstanding = invoices.Sum(i => i.AmountDue);
        var overdueItems = invoices.Where(i => i.IsOverdue).ToList();
        var totalOverdue = overdueItems.Sum(i => i.AmountDue);
        var overdueCount = overdueItems.Count;

        var customerIds = invoices.Select(i => i.CustomerId).Distinct().ToList();
        var customers = await db.Customers
            .AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        var grouped = invoices
            .GroupBy(i => i.CustomerId)
            .Select(g => new ArCustomerSummaryDto(
                g.Key,
                customers.GetValueOrDefault(g.Key, string.Empty),
                g.Sum(i => i.AmountDue),
                g.Where(i => i.IsOverdue).Sum(i => i.AmountDue),
                g.Count()))
            .OrderByDescending(s => s.Overdue)
            .ToList();

        return new ArDashboardDto(businessId, totalOutstanding, totalOverdue, overdueCount, grouped);
    }

    public async Task<SalesInvoiceDto> CreateAsync(CreateSalesInvoiceRequest req, Guid createdByUserId, CancellationToken ct = default)
    {
        var invoice = new SalesInvoice
        {
            Id = Guid.NewGuid(),
            BusinessId = req.BusinessId,
            CustomerId = req.CustomerId,
            SalesOrderId = req.SalesOrderId,
            ChartOfAccountId = req.ChartOfAccountId,
            InvoiceNumber = await GenerateNumberAsync(req.BusinessId, ct),
            CustomerReference = req.CustomerReference,
            Status = SalesInvoiceStatus.Draft,
            InvoiceDate = req.InvoiceDate,
            DueDate = req.DueDate,
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

        var items = BuildItems(req.Items, invoice.Id);
        RecalcHeader(invoice, items);
        invoice.TotalAmountBase = decimal.Round(invoice.TotalAmount * req.ExchangeRate, 2);
        invoice.AmountPaid = 0m;
        invoice.AmountDue = invoice.TotalAmount;

        db.SalesInvoices.Add(invoice);
        db.SalesInvoiceItems.AddRange(items);
        await db.SaveChangesAsync(ct);

        return (await LoadDtoAsync(invoice.Id, ct))!;
    }

    public async Task SendAsync(Guid id, CancellationToken ct = default)
    {
        var invoice = await db.SalesInvoices.FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new InvalidOperationException("Invoice not found.");

        if (invoice.Status != SalesInvoiceStatus.Draft)
            throw new InvalidOperationException("Only draft invoices can be sent.");

        invoice.Status = SalesInvoiceStatus.Sent;
        invoice.SentAtUtc = DateTimeOffset.UtcNow;
        invoice.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<SalesInvoicePaymentDto> RecordPaymentAsync(Guid id, RecordSalesPaymentRequest req, Guid createdByUserId, CancellationToken ct = default)
    {
        var invoice = await db.SalesInvoices
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new InvalidOperationException("Invoice not found.");

        if (invoice.Status is SalesInvoiceStatus.Void)
            throw new InvalidOperationException("Cannot record payment on a void invoice.");
        if (invoice.AmountDue <= 0)
            throw new InvalidOperationException("Invoice is already fully paid.");

        if (req.Amount > invoice.AmountDue)
            throw new InvalidOperationException($"Payment amount ({req.Amount}) exceeds outstanding balance ({invoice.AmountDue}).");

        var payment = new SalesInvoicePayment
        {
            Id = Guid.NewGuid(),
            SalesInvoiceId = id,
            PaymentReference = req.PaymentReference,
            PaymentMethod = req.PaymentMethod,
            Amount = req.Amount,
            CurrencyCode = req.CurrencyCode,
            ExchangeRate = req.ExchangeRate,
            AmountBase = decimal.Round(req.Amount * req.ExchangeRate, 2),
            PaymentDate = req.PaymentDate,
            Notes = req.Notes,
            ReceivedByUserId = createdByUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        invoice.AmountPaid += req.Amount;
        invoice.AmountDue = decimal.Round(invoice.TotalAmount - invoice.AmountPaid, 2);
        invoice.Status = invoice.AmountDue <= 0 ? SalesInvoiceStatus.Paid : SalesInvoiceStatus.PartiallyPaid;
        invoice.DaysOverdue = 0;
        invoice.ModifiedAtUtc = DateTimeOffset.UtcNow;

        db.SalesInvoicePayments.Add(payment);
        await db.SaveChangesAsync(ct);

        return new SalesInvoicePaymentDto(
            payment.Id, payment.PaymentReference, payment.PaymentMethod,
            payment.Amount, payment.CurrencyCode, payment.AmountBase,
            payment.PaymentDate, payment.Notes, payment.CreatedAtUtc);
    }

    public async Task CancelAsync(Guid id, CancellationToken ct = default)
    {
        var invoice = await db.SalesInvoices.FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new InvalidOperationException("Invoice not found.");

        if (invoice.Status is SalesInvoiceStatus.Paid or SalesInvoiceStatus.PartiallyPaid)
            throw new InvalidOperationException("Cannot void a paid invoice. Issue a credit note instead.");

        invoice.Status = SalesInvoiceStatus.Void;
        invoice.VoidedAtUtc = DateTimeOffset.UtcNow;
        invoice.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task RefreshOverdueStatusAsync(Guid businessId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var invoices = await db.SalesInvoices
            .Where(i => i.BusinessId == businessId
                        && i.AmountDue > 0
                        && i.Status != SalesInvoiceStatus.Void
                        && i.Status != SalesInvoiceStatus.Paid)
            .ToListAsync(ct);

        foreach (var invoice in invoices)
        {
            if (invoice.DueDate < today)
            {
                invoice.Status = SalesInvoiceStatus.Overdue;
                invoice.DaysOverdue = (today.ToDateTime(TimeOnly.MinValue) - invoice.DueDate.ToDateTime(TimeOnly.MinValue)).Days;
            }
        }
        await db.SaveChangesAsync(ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private async Task<string> GenerateNumberAsync(Guid businessId, CancellationToken ct)
    {
        var count = await db.SalesInvoices.CountAsync(i => i.BusinessId == businessId, ct);
        return $"INV-{DateTime.UtcNow.Year}-{(count + 1):D5}";
    }

    private async Task<SalesInvoiceDto?> LoadDtoAsync(Guid id, CancellationToken ct)
        => (await db.SalesInvoices.AsNoTracking()
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id, ct)) is { } inv ? MapToDto(inv) : null;

    private static List<SalesInvoiceItem> BuildItems(IEnumerable<CreateSalesInvoiceItemRequest> reqItems, Guid invoiceId)
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
            return new SalesInvoiceItem
            {
                Id = Guid.NewGuid(),
                SalesInvoiceId = invoiceId,
                SalesOrderItemId = i.SalesOrderItemId,
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

    private static void RecalcHeader(SalesInvoice inv, List<SalesInvoiceItem> items)
    {
        inv.SubTotal = items.Sum(i => i.LineTotalAfterDiscount);
        inv.OrderDiscountAmount = inv.OrderDiscountType switch
        {
            SalesDiscountType.Amount => inv.OrderDiscountValue,
            SalesDiscountType.Percentage => decimal.Round(inv.SubTotal * inv.OrderDiscountValue / 100, 4),
            _ => 0m
        };
        inv.TaxAmount = items.Sum(i => i.TaxAmount);
        inv.TotalAmount = decimal.Round(inv.SubTotal - inv.OrderDiscountAmount + inv.ShippingFee + inv.TaxAmount, 2);
    }

    private static SalesInvoiceDto MapToDto(SalesInvoice inv)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var daysOverdue = inv.AmountDue > 0 && inv.DueDate < today
            ? (today.ToDateTime(TimeOnly.MinValue) - inv.DueDate.ToDateTime(TimeOnly.MinValue)).Days
            : 0;

        return new SalesInvoiceDto(
            inv.Id, inv.BusinessId, inv.CustomerId, inv.SalesOrderId,
            inv.InvoiceNumber, inv.CustomerReference, inv.Status.ToString(),
            inv.InvoiceDate, inv.DueDate,
            inv.CurrencyCode, inv.ExchangeRate,
            inv.SubTotal, inv.OrderDiscountAmount, inv.ShippingFee,
            inv.TaxAmount, inv.TotalAmount, inv.TotalAmountBase,
            inv.AmountPaid, inv.AmountDue, daysOverdue,
            inv.Notes,
            inv.SentAtUtc, inv.CreatedAtUtc,
            inv.Items.OrderBy(i => i.SortOrder).Select(i => new SalesInvoiceItemDto(
                i.Id, i.InventoryItemId, i.Description,
                i.Quantity, i.Unit, i.UnitPrice,
                i.DiscountType.ToString(), i.DiscountValue, i.DiscountAmount,
                i.TaxRate, i.TaxAmount, i.LineTotal,
                i.Notes, i.SortOrder)).ToList(),
            inv.Payments.OrderBy(p => p.PaymentDate).Select(p => new SalesInvoicePaymentDto(
                p.Id, p.PaymentReference, p.PaymentMethod,
                p.Amount, p.CurrencyCode, p.AmountBase,
                p.PaymentDate, p.Notes, p.CreatedAtUtc)).ToList());
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ArCreditNoteService
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class ArCreditNoteService(ApplicationDbContext db) : IArCreditNoteService
{
    public async Task<IReadOnlyList<ArCreditNoteDto>> GetByBusinessAsync(
        Guid businessId, Guid? customerId = null, CancellationToken ct = default)
    {
        var query = db.ArCreditNotes.AsNoTracking()
            .Include(c => c.Items)
            .Include(c => c.Applications)
            .Where(c => c.BusinessId == businessId);

        if (customerId.HasValue) query = query.Where(c => c.CustomerId == customerId.Value);

        return (await query.OrderByDescending(c => c.CreditNoteDate).ToListAsync(ct))
            .Select(MapToDto).ToList();
    }

    public async Task<ArCreditNoteDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => (await db.ArCreditNotes.AsNoTracking()
            .Include(c => c.Items)
            .Include(c => c.Applications)
            .FirstOrDefaultAsync(c => c.Id == id, ct)) is { } c ? MapToDto(c) : null;

    public async Task<ArCreditNoteDto> CreateAsync(CreateArCreditNoteRequest req, Guid createdByUserId, CancellationToken ct = default)
    {
        var creditNote = new ArCreditNote
        {
            Id = Guid.NewGuid(),
            BusinessId = req.BusinessId,
            CustomerId = req.CustomerId,
            SalesInvoiceId = req.SalesInvoiceId,
            CreditNoteNumber = await GenerateNumberAsync(req.BusinessId, ct),
            Status = ArCreditNoteStatus.Draft,
            CreditNoteDate = req.CreditNoteDate,
            Reason = req.Reason,
            CurrencyCode = req.CurrencyCode,
            ExchangeRate = req.ExchangeRate,
            Notes = req.Notes,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var items = BuildItems(req.Items, creditNote.Id);
        creditNote.SubTotal = items.Sum(i => i.LineTotal - i.TaxAmount);
        creditNote.TaxAmount = items.Sum(i => i.TaxAmount);
        creditNote.TotalAmount = decimal.Round(creditNote.SubTotal + creditNote.TaxAmount, 2);
        creditNote.TotalAmountBase = decimal.Round(creditNote.TotalAmount * req.ExchangeRate, 2);
        creditNote.RemainingAmount = creditNote.TotalAmount;

        db.ArCreditNotes.Add(creditNote);
        db.ArCreditNoteItems.AddRange(items);
        await db.SaveChangesAsync(ct);

        creditNote.Items = items;
        creditNote.Applications = [];
        return MapToDto(creditNote);
    }

    public async Task ConfirmAsync(Guid id, Guid confirmedByUserId, CancellationToken ct = default)
    {
        var cn = await db.ArCreditNotes.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new InvalidOperationException("Credit note not found.");

        if (cn.Status != ArCreditNoteStatus.Draft)
            throw new InvalidOperationException("Only draft credit notes can be confirmed.");

        cn.Status = ArCreditNoteStatus.Confirmed;
        cn.ConfirmedByUserId = confirmedByUserId;
        cn.ConfirmedAtUtc = DateTimeOffset.UtcNow;
        cn.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<ArCreditNoteApplicationDto> ApplyAsync(Guid creditNoteId, ApplyArCreditNoteRequest req, Guid appliedByUserId, CancellationToken ct = default)
    {
        var cn = await db.ArCreditNotes
            .Include(c => c.Applications)
            .FirstOrDefaultAsync(c => c.Id == creditNoteId, ct)
            ?? throw new InvalidOperationException("Credit note not found.");

        if (cn.Status is not (ArCreditNoteStatus.Confirmed or ArCreditNoteStatus.PartiallyApplied))
            throw new InvalidOperationException("Credit note must be confirmed before applying.");

        if (req.AmountToApply > cn.RemainingAmount)
            throw new InvalidOperationException($"Amount to apply ({req.AmountToApply}) exceeds remaining balance ({cn.RemainingAmount}).");

        var invoice = await db.SalesInvoices.FirstOrDefaultAsync(i => i.Id == req.SalesInvoiceId, ct)
            ?? throw new InvalidOperationException("Invoice not found.");

        if (invoice.BusinessId != cn.BusinessId)
            throw new InvalidOperationException("Invoice does not belong to the same business.");

        if (req.AmountToApply > invoice.AmountDue)
            throw new InvalidOperationException($"Amount ({req.AmountToApply}) exceeds invoice outstanding ({invoice.AmountDue}).");

        var application = new ArCreditNoteApplication
        {
            Id = Guid.NewGuid(),
            ArCreditNoteId = creditNoteId,
            SalesInvoiceId = req.SalesInvoiceId,
            AmountApplied = req.AmountToApply,
            ApplicationDate = req.ApplicationDate,
            Notes = req.Notes,
            AppliedByUserId = appliedByUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        // Reduce invoice outstanding
        invoice.AmountDue = decimal.Round(invoice.AmountDue - req.AmountToApply, 2);
        invoice.AmountPaid += req.AmountToApply;
        if (invoice.AmountDue <= 0)
        {
            invoice.Status = SalesInvoiceStatus.Paid;
            invoice.DaysOverdue = 0;
        }
        else
        {
            invoice.Status = invoice.Status == SalesInvoiceStatus.Overdue
                ? SalesInvoiceStatus.Overdue
                : SalesInvoiceStatus.PartiallyPaid;
        }
        invoice.ModifiedAtUtc = DateTimeOffset.UtcNow;

        // Reduce CN remaining
        cn.RemainingAmount = decimal.Round(cn.RemainingAmount - req.AmountToApply, 2);
        cn.Status = cn.RemainingAmount <= 0 ? ArCreditNoteStatus.Applied : ArCreditNoteStatus.PartiallyApplied;
        cn.ModifiedAtUtc = DateTimeOffset.UtcNow;

        db.ArCreditNoteApplications.Add(application);
        await db.SaveChangesAsync(ct);

        return new ArCreditNoteApplicationDto(
            application.Id, application.SalesInvoiceId,
            application.AmountApplied, application.ApplicationDate, application.Notes,
            application.CreatedAtUtc);
    }

    public async Task CancelAsync(Guid id, CancellationToken ct = default)
    {
        var cn = await db.ArCreditNotes.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new InvalidOperationException("Credit note not found.");

        if (cn.Status is ArCreditNoteStatus.Applied or ArCreditNoteStatus.PartiallyApplied)
            throw new InvalidOperationException("Cannot cancel a credit note that has been applied to invoices.");

        cn.Status = ArCreditNoteStatus.Cancelled;
        cn.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private async Task<string> GenerateNumberAsync(Guid businessId, CancellationToken ct)
    {
        var count = await db.ArCreditNotes.CountAsync(c => c.BusinessId == businessId, ct);
        return $"CRN-{DateTime.UtcNow.Year}-{(count + 1):D5}";
    }

    private static List<ArCreditNoteItem> BuildItems(IEnumerable<CreateArCreditNoteItemRequest> reqItems, Guid cnId)
        => reqItems.Select(i =>
        {
            var lineTotal = i.Quantity * i.UnitPrice;
            var taxAmount = decimal.Round(lineTotal * i.TaxRate, 4);
            return new ArCreditNoteItem
            {
                Id = Guid.NewGuid(),
                ArCreditNoteId = cnId,
                InventoryItemId = i.InventoryItemId,
                Description = i.Description,
                Quantity = i.Quantity,
                Unit = i.Unit,
                UnitPrice = i.UnitPrice,
                TaxRate = i.TaxRate,
                TaxAmount = taxAmount,
                LineTotal = lineTotal + taxAmount,
                Notes = i.Notes,
                SortOrder = i.SortOrder
            };
        }).ToList();

    private static ArCreditNoteDto MapToDto(ArCreditNote c) => new(
        c.Id, c.BusinessId, c.CustomerId, c.SalesInvoiceId,
        c.CreditNoteNumber, c.Status.ToString(), c.Reason,
        c.CreditNoteDate,
        c.CurrencyCode, c.ExchangeRate,
        c.SubTotal, c.TaxAmount, c.TotalAmount, c.TotalAmountBase,
        c.RemainingAmount,
        c.Notes,
        c.ConfirmedAtUtc, c.CreatedAtUtc,
        c.Items.OrderBy(i => i.SortOrder).Select(i => new ArCreditNoteItemDto(
            i.Id, i.InventoryItemId, i.Description,
            i.Quantity, i.Unit, i.UnitPrice,
            i.TaxRate, i.TaxAmount, i.LineTotal,
            i.Notes, i.SortOrder)).ToList(),
        c.Applications.OrderBy(a => a.ApplicationDate).Select(a => new ArCreditNoteApplicationDto(
            a.Id, a.SalesInvoiceId,
            a.AmountApplied, a.ApplicationDate, a.Notes,
            a.CreatedAtUtc)).ToList());
}

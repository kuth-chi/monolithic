using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;
using Monolithic.Api.Modules.Purchases.Contracts;
using Monolithic.Api.Modules.Purchases.Domain;

namespace Monolithic.Api.Modules.Purchases.Application;

public sealed class PurchaseReturnService(ApplicationDbContext db) : IPurchaseReturnService
{
    public async Task<IReadOnlyList<PurchaseReturnDto>> GetByBusinessAsync(
        Guid businessId, Guid? vendorId = null, string? status = null, CancellationToken ct = default)
    {
        var query = db.PurchaseReturns.AsNoTracking()
            .Include(r => r.Items)
            .Where(r => r.BusinessId == businessId);

        if (vendorId.HasValue) query = query.Where(r => r.VendorId == vendorId.Value);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PurchaseReturnStatus>(status, true, out var s))
            query = query.Where(r => r.Status == s);

        return (await query.OrderByDescending(r => r.ReturnDate).ToListAsync(ct))
            .Select(MapToDto).ToList();
    }

    public async Task<PurchaseReturnDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => (await db.PurchaseReturns.AsNoTracking()
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id, ct)) is { } r ? MapToDto(r) : null;

    public async Task<PurchaseReturnDto> CreateAsync(CreatePurchaseReturnRequest req, Guid createdByUserId, CancellationToken ct = default)
    {
        var @return = new PurchaseReturn
        {
            Id = Guid.NewGuid(),
            BusinessId = req.BusinessId,
            VendorId = req.VendorId,
            PurchaseOrderId = req.PurchaseOrderId,
            VendorBillId = req.VendorBillId,
            ReturnNumber = await GenerateNumberAsync(req.BusinessId, ct),
            Status = PurchaseReturnStatus.Draft,
            ReturnDate = req.ReturnDate,
            Reason = req.Reason,
            CurrencyCode = req.CurrencyCode,
            ExchangeRate = req.ExchangeRate,
            Notes = req.Notes,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var items = req.Items.Select(i =>
        {
            var lineTotal = i.Quantity * i.UnitPrice;
            var taxAmount = decimal.Round(lineTotal * i.TaxRate, 4);
            return new PurchaseReturnItem
            {
                Id = Guid.NewGuid(),
                PurchaseReturnId = @return.Id,
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

        @return.TotalAmount = decimal.Round(items.Sum(i => i.LineTotal), 2);
        @return.TotalAmountBase = decimal.Round(@return.TotalAmount * req.ExchangeRate, 2);

        db.PurchaseReturns.Add(@return);
        db.PurchaseReturnItems.AddRange(items);
        await db.SaveChangesAsync(ct);

        @return.Items = items;
        return MapToDto(@return);
    }

    public async Task ConfirmAsync(Guid id, Guid confirmedByUserId, CancellationToken ct = default)
    {
        var @return = await db.PurchaseReturns.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new InvalidOperationException("Purchase return not found.");

        if (@return.Status != PurchaseReturnStatus.Draft)
            throw new InvalidOperationException("Only draft returns can be confirmed.");

        @return.Status = PurchaseReturnStatus.Confirmed;
        @return.ConfirmedByUserId = confirmedByUserId;
        @return.ConfirmedAtUtc = DateTimeOffset.UtcNow;
        @return.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task MarkShippedAsync(Guid id, CancellationToken ct = default)
    {
        var @return = await db.PurchaseReturns.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new InvalidOperationException("Purchase return not found.");

        if (@return.Status != PurchaseReturnStatus.Confirmed)
            throw new InvalidOperationException("Only confirmed returns can be marked as shipped.");

        @return.Status = PurchaseReturnStatus.Shipped;
        @return.ShippedAtUtc = DateTimeOffset.UtcNow;
        @return.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task RecordVendorCreditAsync(Guid id, RecordVendorCreditRequest req, CancellationToken ct = default)
    {
        var @return = await db.PurchaseReturns.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new InvalidOperationException("Purchase return not found.");

        if (@return.Status != PurchaseReturnStatus.Shipped)
            throw new InvalidOperationException("Return must be shipped before recording vendor credit.");

        @return.Status = PurchaseReturnStatus.Credited;
        @return.VendorCreditNoteReference = req.VendorCreditNoteReference;
        @return.Notes = string.IsNullOrWhiteSpace(req.Notes) ? @return.Notes : $"{@return.Notes}\n{req.Notes}".Trim();
        @return.CreditedAtUtc = DateTimeOffset.UtcNow;
        @return.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task CancelAsync(Guid id, CancellationToken ct = default)
    {
        var @return = await db.PurchaseReturns.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new InvalidOperationException("Purchase return not found.");

        if (@return.Status is PurchaseReturnStatus.Shipped or PurchaseReturnStatus.Credited)
            throw new InvalidOperationException("Cannot cancel a shipped or credited return.");

        @return.Status = PurchaseReturnStatus.Cancelled;
        @return.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private async Task<string> GenerateNumberAsync(Guid businessId, CancellationToken ct)
    {
        var count = await db.PurchaseReturns.CountAsync(r => r.BusinessId == businessId, ct);
        return $"PRN-{DateTime.UtcNow.Year}-{(count + 1):D5}";
    }

    private static PurchaseReturnDto MapToDto(PurchaseReturn r) => new(
        r.Id, r.BusinessId, r.VendorId,
        r.PurchaseOrderId, r.VendorBillId,
        r.ReturnNumber, r.Status.ToString(),
        r.ReturnDate, r.Reason,
        r.CurrencyCode, r.ExchangeRate,
        r.TotalAmount, r.TotalAmountBase,
        r.VendorCreditNoteReference, r.Notes,
        r.ConfirmedAtUtc, r.ShippedAtUtc, r.CreditedAtUtc, r.CreatedAtUtc,
        r.Items.OrderBy(i => i.SortOrder).Select(i => new PurchaseReturnItemDto(
            i.Id, i.InventoryItemId, i.Description,
            i.Quantity, i.Unit, i.UnitPrice,
            i.TaxRate, i.TaxAmount, i.LineTotal,
            i.Notes, i.SortOrder)).ToList());
}

namespace Monolithic.Api.Modules.Purchases.Domain;

/// <summary>
/// Purchase Return — goods returned to a vendor against an original purchase order or vendor bill.
/// Flow: Draft → Confirmed → Shipped → Credited | Cancelled.
/// On Confirmed, inventory stock is decremented; on Cancelled the quantity is restored.
/// </summary>
public class PurchaseReturn
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid VendorId { get; set; }

    /// <summary>Original purchase order (if returning against a PO).</summary>
    public Guid? PurchaseOrderId { get; set; }

    /// <summary>Vendor bill being credited (if returning against a bill).</summary>
    public Guid? VendorBillId { get; set; }

    /// <summary>Auto-generated reference, e.g. PRN-2026-00001.</summary>
    public string ReturnNumber { get; set; } = string.Empty;

    public PurchaseReturnStatus Status { get; set; } = PurchaseReturnStatus.Draft;

    public DateOnly ReturnDate { get; set; }

    public string Reason { get; set; } = string.Empty;

    // ── Currency ──────────────────────────────────────────────────────────────
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;

    // ── Amounts ───────────────────────────────────────────────────────────────
    public decimal TotalAmount { get; set; }
    public decimal TotalAmountBase { get; set; }

    /// <summary>Vendor credit note / debit note reference from the vendor.</summary>
    public string VendorCreditNoteReference { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public Guid? CreatedByUserId { get; set; }
    public Guid? ConfirmedByUserId { get; set; }
    public DateTimeOffset? ConfirmedAtUtc { get; set; }
    public DateTimeOffset? ShippedAtUtc { get; set; }
    public DateTimeOffset? CreditedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual ICollection<PurchaseReturnItem> Items { get; set; } = [];
}

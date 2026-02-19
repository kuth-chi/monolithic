namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Vendor Bill (Accounts Payable Invoice).
/// Represents a bill received from a vendor for goods/services ordered via a PO.
/// One PO can generate multiple partial bills; one bill can cover multiple POs.
/// 
/// Overdue logic: Status becomes Overdue when DueDateUtc &lt; today and AmountDue &gt; 0.
/// Run a background job or query: WHERE DueDate &lt; NOW() AND AmountDue &gt; 0 AND Status IN (Open, PartiallyPaid)
/// </summary>
public class VendorBill
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    public Guid VendorId { get; set; }

    /// <summary>Primary PO this bill is associated with (nullable for non-PO bills).</summary>
    public Guid? PurchaseOrderId { get; set; }

    /// <summary>AP account from Chart of Accounts (e.g. "Accounts Payable 2000").</summary>
    public Guid? ChartOfAccountId { get; set; }

    /// <summary>Unique bill reference number (generated internally).</summary>
    public string BillNumber { get; set; } = string.Empty;

    /// <summary>Vendor's own invoice reference number.</summary>
    public string VendorInvoiceNumber { get; set; } = string.Empty;

    public VendorBillStatus Status { get; set; } = VendorBillStatus.Draft;

    // ── Dates ─────────────────────────────────────────────────────────────────
    public DateOnly BillDate { get; set; }

    /// <summary>Payment due date (derived from vendor payment terms + bill date).</summary>
    public DateOnly DueDate { get; set; }

    // ── Currency ──────────────────────────────────────────────────────────────
    public string CurrencyCode { get; set; } = "USD";

    public decimal ExchangeRate { get; set; } = 1m;

    // ── Amounts ───────────────────────────────────────────────────────────────
    public decimal SubTotal { get; set; }

    public DiscountType OrderDiscountType { get; set; } = DiscountType.None;

    public decimal OrderDiscountValue { get; set; }

    public decimal OrderDiscountAmount { get; set; }

    public decimal ShippingFee { get; set; }

    public decimal TaxAmount { get; set; }

    /// <summary>Total amount billed.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Total converted to base currency.</summary>
    public decimal TotalAmountBase { get; set; }

    /// <summary>Total payments applied so far.</summary>
    public decimal AmountPaid { get; set; }

    /// <summary>Remaining balance (TotalAmount - AmountPaid). Drives overdue detection.</summary>
    public decimal AmountDue { get; set; }

    /// <summary>Days past due (0 if not overdue). Computed externally or via migration.</summary>
    public int DaysOverdue { get; set; }

    // ── Misc ──────────────────────────────────────────────────────────────────
    public string Notes { get; set; } = string.Empty;

    public string InternalNotes { get; set; } = string.Empty;

    public Guid? CreatedByUserId { get; set; }

    public Guid? ApprovedByUserId { get; set; }

    public DateTimeOffset? ApprovedAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual Business Business { get; set; } = null!;

    public virtual Vendor Vendor { get; set; } = null!;

    public virtual PurchaseOrder? PurchaseOrder { get; set; }

    public virtual ChartOfAccount? ChartOfAccount { get; set; }

    public virtual ICollection<VendorBillItem> Items { get; set; } = [];

    public virtual ICollection<VendorBillPayment> Payments { get; set; } = [];

    /// <summary>Credit note applications that reduced this bill's AmountDue.</summary>
    public virtual ICollection<ApCreditNoteApplication> CreditNoteApplications { get; set; } = [];

    /// <summary>Pay-later schedules targeting this bill.</summary>
    public virtual ICollection<ApPaymentSchedule> PaymentSchedules { get; set; } = [];
}

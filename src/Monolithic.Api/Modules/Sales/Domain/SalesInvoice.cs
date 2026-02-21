namespace Monolithic.Api.Modules.Sales.Domain;

/// <summary>
/// Sales (AR) Invoice — bill sent to customer for goods or services.
/// Represents an Accounts Receivable document.
/// Flow: Draft → Sent → PartiallyPaid | Paid | Overdue → Void.
/// </summary>
public class SalesInvoice
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid CustomerId { get; set; }

    /// <summary>Optional source sales order.</summary>
    public Guid? SalesOrderId { get; set; }

    /// <summary>AR account from Chart of Accounts (e.g. "Accounts Receivable 1200").</summary>
    public Guid? ChartOfAccountId { get; set; }

    /// <summary>Auto-generated reference, e.g. INV-2026-00001.</summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>Customer's own PO/reference number.</summary>
    public string CustomerReference { get; set; } = string.Empty;

    public SalesInvoiceStatus Status { get; set; } = SalesInvoiceStatus.Draft;

    // ── Dates ─────────────────────────────────────────────────────────────────
    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }

    // ── Currency ──────────────────────────────────────────────────────────────
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;

    // ── Amounts ───────────────────────────────────────────────────────────────
    public decimal SubTotal { get; set; }
    public SalesDiscountType OrderDiscountType { get; set; } = SalesDiscountType.None;
    public decimal OrderDiscountValue { get; set; }
    public decimal OrderDiscountAmount { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalAmountBase { get; set; }
    public decimal AmountPaid { get; set; }

    /// <summary>Outstanding balance. Drives overdue detection.</summary>
    public decimal AmountDue { get; set; }

    /// <summary>Days past due date (0 if not yet overdue).</summary>
    public int DaysOverdue { get; set; }

    public string Notes { get; set; } = string.Empty;
    public string TermsAndConditions { get; set; } = string.Empty;

    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? SentAtUtc { get; set; }
    public DateTimeOffset? VoidedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual ICollection<SalesInvoiceItem> Items { get; set; } = [];
    public virtual ICollection<SalesInvoicePayment> Payments { get; set; } = [];
    public virtual ICollection<ArCreditNoteApplication> CreditNoteApplications { get; set; } = [];
}

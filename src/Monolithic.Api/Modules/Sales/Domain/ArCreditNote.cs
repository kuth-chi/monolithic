namespace Monolithic.Api.Modules.Sales.Domain;

/// <summary>
/// AR Credit Note — issued to a customer for returns, price adjustments, or refunds.
/// Flow: Draft → Confirmed → PartiallyApplied | Applied → Cancelled.
/// Applied credit reduces the customer's invoice AmountDue.
/// </summary>
public class ArCreditNote
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid CustomerId { get; set; }

    /// <summary>Optional: original invoice that is being credited.</summary>
    public Guid? SalesInvoiceId { get; set; }

    /// <summary>Auto-generated reference, e.g. CRN-2026-00001.</summary>
    public string CreditNoteNumber { get; set; } = string.Empty;

    public ArCreditNoteStatus Status { get; set; } = ArCreditNoteStatus.Draft;

    public DateOnly CreditNoteDate { get; set; }

    public string Reason { get; set; } = string.Empty;

    // ── Currency ──────────────────────────────────────────────────────────────
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;

    // ── Amounts ───────────────────────────────────────────────────────────────
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalAmountBase { get; set; }

    /// <summary>Amount not yet applied to invoices.</summary>
    public decimal RemainingAmount { get; set; }

    public string Notes { get; set; } = string.Empty;

    public Guid? CreatedByUserId { get; set; }
    public Guid? ConfirmedByUserId { get; set; }
    public DateTimeOffset? ConfirmedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual ICollection<ArCreditNoteItem> Items { get; set; } = [];
    public virtual ICollection<ArCreditNoteApplication> Applications { get; set; } = [];
}

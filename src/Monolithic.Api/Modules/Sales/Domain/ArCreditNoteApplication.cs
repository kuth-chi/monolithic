namespace Monolithic.Api.Modules.Sales.Domain;

/// <summary>
/// Records the application of an AR credit note amount to a specific sales invoice.
/// Reduces the invoice's AmountDue and the credit note's RemainingAmount.
/// </summary>
public class ArCreditNoteApplication
{
    public Guid Id { get; set; }
    public Guid ArCreditNoteId { get; set; }
    public Guid SalesInvoiceId { get; set; }

    public decimal AmountApplied { get; set; }

    public DateOnly ApplicationDate { get; set; }
    public string Notes { get; set; } = string.Empty;

    public Guid? AppliedByUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual ArCreditNote ArCreditNote { get; set; } = null!;
    public virtual SalesInvoice SalesInvoice { get; set; } = null!;
}

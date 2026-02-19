namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Records how a credit note amount was applied against a specific vendor bill,
/// reducing the bill's AmountDue.
/// </summary>
public class ApCreditNoteApplication
{
    public Guid Id { get; set; }

    public Guid CreditNoteId { get; set; }

    public Guid VendorBillId { get; set; }

    /// <summary>Amount of credit applied to this bill.</summary>
    public decimal AppliedAmount { get; set; }

    public DateOnly ApplicationDate { get; set; }

    public string Notes { get; set; } = string.Empty;

    public Guid? CreatedByUserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual ApCreditNote CreditNote { get; set; } = null!;

    public virtual VendorBill VendorBill { get; set; } = null!;
}

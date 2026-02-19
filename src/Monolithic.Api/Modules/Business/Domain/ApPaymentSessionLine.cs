namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// One bill allocation within an ApPaymentSession.
/// For BulkBillPayment: lines are auto-generated (oldest first).
/// For SelectedBillPayment: lines are provided by the caller.
/// </summary>
public class ApPaymentSessionLine
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    public Guid VendorBillId { get; set; }

    /// <summary>Amount allocated to this bill from the session total.</summary>
    public decimal AllocatedAmount { get; set; }

    /// <summary>Amount due on the bill before this allocation.</summary>
    public decimal BillAmountDueBefore { get; set; }

    /// <summary>Amount due on the bill after this allocation.</summary>
    public decimal BillAmountDueAfter { get; set; }

    /// <summary>True when AllocatedAmount &lt; BillAmountDueBefore (partial payment).</summary>
    public bool IsPartialPayment { get; set; }

    /// <summary>FK to the VendorBillPayment record created when the session is posted.</summary>
    public Guid? VendorBillPaymentId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual ApPaymentSession Session { get; set; } = null!;

    public virtual VendorBill VendorBill { get; set; } = null!;

    public virtual VendorBillPayment? VendorBillPayment { get; set; }
}

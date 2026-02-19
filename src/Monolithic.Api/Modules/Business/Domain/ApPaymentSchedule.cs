namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Pay-Later schedule — allows an AP manager to schedule a future payment for a bill.
/// A payment run job or manual trigger converts a Scheduled entry into an ApPaymentSession.
/// Multiple schedules can exist for the same bill (installment structure).
/// </summary>
public class ApPaymentSchedule
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    public Guid VendorId { get; set; }

    public Guid VendorBillId { get; set; }

    public ApPaymentScheduleStatus Status { get; set; } = ApPaymentScheduleStatus.Scheduled;

    /// <summary>Date on which the payment should be executed.</summary>
    public DateOnly ScheduledDate { get; set; }

    public decimal ScheduledAmount { get; set; }

    public string CurrencyCode { get; set; } = "USD";

    /// <summary>Bank account to pay from.</summary>
    public Guid? BankAccountId { get; set; }

    public string PaymentMethod { get; set; } = "BankTransfer";

    public string Notes { get; set; } = string.Empty;

    /// <summary>The payment session created when this schedule was executed.</summary>
    public Guid? ExecutedSessionId { get; set; }

    public DateTimeOffset? ExecutedAtUtc { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual Business Business { get; set; } = null!;

    public virtual Vendor Vendor { get; set; } = null!;

    public virtual VendorBill VendorBill { get; set; } = null!;

    public virtual ApPaymentSession? ExecutedSession { get; set; }
}

public enum ApPaymentScheduleStatus
{
    Scheduled = 0,
    Executed = 1,
    Cancelled = 2,
    Overdue = 3   // ScheduledDate passed but not yet executed
}

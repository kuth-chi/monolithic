namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// AP Payment Session — a single payment run that can settle one or more vendor bills
/// in one transaction. Supports two modes:
///   1. BulkBillPayment  — caller provides a total amount and the system auto-allocates
///      to bills oldest-first until the amount is exhausted.
///   2. SelectedBillPayment — caller explicitly selects bills; the system validates and
///      applies payment. Partial payment on the last selected bill is allowed.
///
/// The session is immutable once Posted. Each allocation line is in ApPaymentSessionLine.
/// </summary>
public class ApPaymentSession
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    public Guid VendorId { get; set; }

    public ApPaymentMode PaymentMode { get; set; } = ApPaymentMode.SelectedBillPayment;

    public ApPaymentSessionStatus Status { get; set; } = ApPaymentSessionStatus.Draft;

    /// <summary>Reference / voucher number for this payment run.</summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>Bank account used to make the payment.</summary>
    public Guid? BankAccountId { get; set; }

    /// <summary>Total amount being paid in session currency.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Total amount in business base currency.</summary>
    public decimal TotalAmountBase { get; set; }

    public string CurrencyCode { get; set; } = "USD";

    public decimal ExchangeRate { get; set; } = 1m;

    public string PaymentMethod { get; set; } = "BankTransfer";

    public DateOnly PaymentDate { get; set; }

    public string Notes { get; set; } = string.Empty;

    public Guid? CreatedByUserId { get; set; }

    public Guid? PostedByUserId { get; set; }

    public DateTimeOffset? PostedAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual Business Business { get; set; } = null!;

    public virtual Vendor Vendor { get; set; } = null!;

    public virtual BankAccountBase? BankAccount { get; set; }

    public virtual ICollection<ApPaymentSessionLine> Lines { get; set; } = [];
}

/// <summary>
/// How the payment session distributes the total payment amount across bills.
/// </summary>
public enum ApPaymentMode
{
    /// <summary>
    /// System auto-allocates from the total amount to bills ordered oldest DueDate first.
    /// Last bill may receive a partial payment.
    /// </summary>
    BulkBillPayment = 0,

    /// <summary>
    /// Caller selects specific bills. System validates total == sum of allocations.
    /// Partial payment on the last bill is allowed when AllowPartial=true.
    /// </summary>
    SelectedBillPayment = 1
}

/// <summary>Lifecycle of an AP payment session.</summary>
public enum ApPaymentSessionStatus
{
    Draft = 0,
    Posted = 1,   // Payment applied; VendorBillPayments created
    Reversed = 2  // Full reversal applied (credit notes restored)
}

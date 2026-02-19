namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Records a payment applied against a Vendor Bill.
/// A bill may have multiple partial payments.
/// </summary>
public class VendorBillPayment
{
    public Guid Id { get; set; }

    public Guid VendorBillId { get; set; }

    /// <summary>Bank account the payment was made from.</summary>
    public Guid? BankAccountId { get; set; }

    /// <summary>Payment amount in the bill's currency.</summary>
    public decimal Amount { get; set; }

    /// <summary>Payment amount in business base currency.</summary>
    public decimal AmountBase { get; set; }

    public string CurrencyCode { get; set; } = "USD";

    public decimal ExchangeRate { get; set; } = 1m;

    public DateOnly PaymentDate { get; set; }

    /// <summary>Payment method: "BankTransfer", "Cash", "Cheque", "CreditCard".</summary>
    public string PaymentMethod { get; set; } = "BankTransfer";

    /// <summary>Bank transaction reference / cheque number.</summary>
    public string Reference { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public Guid? CreatedByUserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual VendorBill VendorBill { get; set; } = null!;

    public virtual BankAccountBase? BankAccount { get; set; }
}

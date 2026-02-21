namespace Monolithic.Api.Modules.Sales.Domain;

/// <summary>
/// Records a payment received against a customer (AR) invoice.
/// Immutable after creation — void or reverse via a new credit transaction.
/// </summary>
public class SalesInvoicePayment
{
    public Guid Id { get; set; }
    public Guid SalesInvoiceId { get; set; }

    public string PaymentReference { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "BankTransfer";

    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;
    public decimal AmountBase { get; set; }

    public DateOnly PaymentDate { get; set; }
    public string Notes { get; set; } = string.Empty;

    public Guid? ReceivedByUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual SalesInvoice SalesInvoice { get; set; } = null!;
}

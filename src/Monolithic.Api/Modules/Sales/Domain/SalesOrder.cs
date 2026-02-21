namespace Monolithic.Api.Modules.Sales.Domain;

/// <summary>
/// Sales Order — confirmed agreement to supply goods/services.
/// Flow: Draft → Confirmed → InProgress → Invoiced → Completed | Cancelled.
/// </summary>
public class SalesOrder
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid CustomerId { get; set; }

    /// <summary>Optional: source quotation that was converted.</summary>
    public Guid? QuotationId { get; set; }

    /// <summary>Auto-generated reference, e.g. SO-2026-00001.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    public SalesOrderStatus Status { get; set; } = SalesOrderStatus.Draft;

    public DateOnly OrderDate { get; set; }

    /// <summary>Expected delivery / fulfilment date.</summary>
    public DateOnly? ExpectedDeliveryDate { get; set; }

    // ── Delivery ──────────────────────────────────────────────────────────────
    public string DeliveryAddress { get; set; } = string.Empty;
    public string ShippingMethod { get; set; } = string.Empty;

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

    public string Notes { get; set; } = string.Empty;
    public string TermsAndConditions { get; set; } = string.Empty;

    public Guid? CreatedByUserId { get; set; }
    public Guid? ConfirmedByUserId { get; set; }
    public DateTimeOffset? ConfirmedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual ICollection<SalesOrderItem> Items { get; set; } = [];
    public virtual ICollection<SalesInvoice> Invoices { get; set; } = [];
}

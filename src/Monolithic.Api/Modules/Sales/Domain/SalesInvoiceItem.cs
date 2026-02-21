namespace Monolithic.Api.Modules.Sales.Domain;

/// <summary>A line item on a customer (AR) invoice.</summary>
public class SalesInvoiceItem
{
    public Guid Id { get; set; }
    public Guid SalesInvoiceId { get; set; }
    public Guid? SalesOrderItemId { get; set; }

    public Guid? InventoryItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "pieces";
    public decimal UnitPrice { get; set; }

    public SalesDiscountType DiscountType { get; set; } = SalesDiscountType.None;
    public decimal DiscountValue { get; set; }
    public decimal DiscountAmount { get; set; }

    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }

    public decimal LineTotalBeforeDiscount { get; set; }
    public decimal LineTotalAfterDiscount { get; set; }
    public decimal LineTotal { get; set; }

    public string Notes { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual SalesInvoice SalesInvoice { get; set; } = null!;
}

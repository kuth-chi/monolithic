namespace Monolithic.Api.Modules.Sales.Domain;

/// <summary>A line item on a customer quotation.</summary>
public class QuotationItem
{
    public Guid Id { get; set; }
    public Guid QuotationId { get; set; }

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
    public virtual Quotation Quotation { get; set; } = null!;
}

namespace Monolithic.Api.Modules.Purchases.Domain;

/// <summary>A line item on a purchase return.</summary>
public class PurchaseReturnItem
{
    public Guid Id { get; set; }
    public Guid PurchaseReturnId { get; set; }

    public Guid? InventoryItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "pieces";
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }

    public string Notes { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual PurchaseReturn PurchaseReturn { get; set; } = null!;
}

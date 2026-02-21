namespace Monolithic.Api.Modules.Sales.Domain;

/// <summary>A line item on an AR credit note.</summary>
public class ArCreditNoteItem
{
    public Guid Id { get; set; }
    public Guid ArCreditNoteId { get; set; }

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
    public virtual ArCreditNote ArCreditNote { get; set; } = null!;
}

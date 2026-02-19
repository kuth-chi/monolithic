namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Immutable ledger entry recording the cost of each inventory movement.
/// Powers FIFO layers, weighted average recalculation, and COGS reporting.
/// </summary>
public class CostLedgerEntry
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    public Guid InventoryItemId { get; set; }

    public Guid? InventoryItemVariantId { get; set; }

    /// <summary>What triggered this entry.</summary>
    public CostLedgerEntryType EntryType { get; set; }

    /// <summary>Reference to the source document (PO ID, bill ID, adjustment ID, etc.).</summary>
    public Guid? ReferenceId { get; set; }

    /// <summary>Human-readable reference number (PO number, bill number, etc.).</summary>
    public string ReferenceNumber { get; set; } = string.Empty;

    // ── Quantity ──────────────────────────────────────────────────────────────
    /// <summary>Positive = stock in; negative = stock out.</summary>
    public decimal Quantity { get; set; }

    // ── Costing ───────────────────────────────────────────────────────────────
    /// <summary>Unit cost at the time of this movement (in base currency).</summary>
    public decimal UnitCost { get; set; }

    /// <summary>Total cost of this entry (Quantity × UnitCost).</summary>
    public decimal TotalCost { get; set; }

    /// <summary>Running weighted-average unit cost after this entry.</summary>
    public decimal AverageUnitCostAfter { get; set; }

    /// <summary>Running stock quantity after this entry.</summary>
    public decimal StockQuantityAfter { get; set; }

    /// <summary>Running stock value after this entry (for balance sheet).</summary>
    public decimal StockValueAfter { get; set; }

    // ── FIFO Layer ────────────────────────────────────────────────────────────
    /// <summary>
    /// For FIFO: remaining unconsumed quantity in this purchase layer.
    /// Null for non-FIFO or consumption entries.
    /// </summary>
    public decimal? FifoLayerRemaining { get; set; }

    /// <summary>Reference to the purchase layer being consumed (FIFO out entries).</summary>
    public Guid? FifoSourceLayerId { get; set; }

    public DateTimeOffset EntryDateUtc { get; set; } = DateTimeOffset.UtcNow;

    public Guid? CreatedByUserId { get; set; }

    public string Notes { get; set; } = string.Empty;

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual Business Business { get; set; } = null!;

    public virtual Inventory.Domain.InventoryItem InventoryItem { get; set; } = null!;

    public virtual Inventory.Domain.InventoryItemVariant? InventoryItemVariant { get; set; }
}

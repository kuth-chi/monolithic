namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Type of cost ledger entry — what triggered the cost movement.
/// </summary>
public enum CostLedgerEntryType
{
    PurchaseReceipt = 1,        // Goods received from vendor PO
    PurchaseReturn = 2,         // Goods returned to vendor
    SaleShipment = 3,           // Cost of goods sold on sales
    SaleReturn = 4,             // Customer return reversal
    InventoryAdjustment = 5,    // Manual stock adjustment
    StockTransfer = 6,          // Warehouse-to-warehouse transfer
    Revaluation = 7,            // Periodic revaluation (standard cost update)
    LandedCost = 8              // Additional landed cost allocation
}

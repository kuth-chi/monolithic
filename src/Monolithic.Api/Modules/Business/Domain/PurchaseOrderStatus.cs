namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Life-cycle status for Purchase Orders.
/// Flow: Draft → PendingApproval → Approved → PartiallyReceived → FullyReceived → Billed → Closed
/// Side exits: OnHold (paused), Cancelled
/// </summary>
public enum PurchaseOrderStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    PartiallyReceived = 3,
    FullyReceived = 4,
    Billed = 5,
    Closed = 6,
    OnHold = 7,
    Cancelled = 8
}

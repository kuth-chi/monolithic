namespace Monolithic.Api.Modules.Purchases.Domain;

/// <summary>Purchase return workflow status.</summary>
public enum PurchaseReturnStatus
{
    Draft     = 0,
    Confirmed = 1,
    Shipped   = 2,   // goods returned to vendor
    Credited  = 3,   // vendor credit note / refund received
    Cancelled = 4
}

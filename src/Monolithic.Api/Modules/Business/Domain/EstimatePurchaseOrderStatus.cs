namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Status of an Estimate Purchase Order (Request for Quotation).
/// </summary>
public enum EstimatePurchaseOrderStatus
{
    Draft = 0,
    SentToVendor = 1,
    VendorQuoteReceived = 2,
    Approved = 3,
    ConvertedToPo = 4,
    Expired = 5,
    Cancelled = 6
}

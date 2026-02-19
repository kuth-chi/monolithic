namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Lifecycle status of a Vendor Bill (AP Invoice).
/// </summary>
public enum VendorBillStatus
{
    Draft = 0,
    Open = 1,           // Bill confirmed, awaiting payment
    PartiallyPaid = 2,
    Paid = 3,
    Overdue = 4,        // Past DueDate with unpaid balance
    Disputed = 5,       // Amount is contested with vendor
    Cancelled = 6,
    Voided = 7
}

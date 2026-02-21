namespace Monolithic.Api.Modules.Sales.Domain;

/// <summary>Workflow status of a customer quotation.</summary>
public enum QuotationStatus
{
    Draft     = 0,
    Sent      = 1,
    Accepted  = 2,
    Rejected  = 3,
    Expired   = 4,
    Converted = 5   // converted to a SalesOrder
}

/// <summary>Workflow status of a sales order.</summary>
public enum SalesOrderStatus
{
    Draft     = 0,
    Confirmed = 1,
    InProgress = 2,
    Invoiced  = 3,
    Completed = 4,
    Cancelled = 5
}

/// <summary>Workflow status of a customer (AR) invoice.</summary>
public enum SalesInvoiceStatus
{
    Draft         = 0,
    Sent          = 1,
    PartiallyPaid = 2,
    Paid          = 3,
    Overdue       = 4,
    Void          = 5
}

/// <summary>Workflow status of an AR credit note.</summary>
public enum ArCreditNoteStatus
{
    Draft            = 0,
    Confirmed        = 1,
    PartiallyApplied = 2,
    Applied          = 3,
    Cancelled        = 4
}

/// <summary>Mirror of AP discount type for line-item and order-level discounts.</summary>
public enum SalesDiscountType
{
    None       = 0,
    Amount     = 1,
    Percentage = 2
}

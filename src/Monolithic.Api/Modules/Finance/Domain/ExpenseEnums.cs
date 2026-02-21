namespace Monolithic.Api.Modules.Finance.Domain;

/// <summary>Expense report workflow status.</summary>
public enum ExpenseStatus
{
    Draft     = 0,
    Submitted = 1,
    Approved  = 2,
    Rejected  = 3,
    Paid      = 4,
    Cancelled = 5
}

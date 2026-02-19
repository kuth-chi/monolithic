namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Sub-classification of an account used for reporting and auto-posting logic.
/// 
/// Best-practice COA number ranges:
///   1000–1999  Current Assets       (Cash, Bank, AR, Inventory, Prepaid)
///   2000–2999  Fixed Assets         (Equipment, Vehicles, Accum. Depreciation)
///   3000–3999  Current Liabilities  (AP, Accrued, VAT Payable, Deferred Revenue)
///   4000–4999  Long-term Liabilities(Bank Loan, Bonds)
///   5000–5999  Equity               (Owner Capital, Retained Earnings)
///   6000–6999  Operating Revenue    (Sales, Service Revenue)
///   7000–7999  Cost of Goods Sold   (COGS, Direct Labour, Freight-in)
///   8000–8999  Operating Expenses   (Rent, Salaries, Utilities, Marketing)
///   9000–9999  Tax / Other          (Income Tax Expense, Interest Expense)
/// </summary>
public enum AccountCategory
{
    // Assets
    CurrentAsset = 10,
    FixedAsset = 11,
    ContraAsset = 12,           // Accumulated depreciation, allowance for doubtful accounts

    // Liabilities
    CurrentLiability = 20,
    LongTermLiability = 21,

    // Equity
    OwnersEquity = 30,
    RetainedEarnings = 31,

    // Revenue
    OperatingRevenue = 40,
    OtherRevenue = 41,

    // Expenses
    CostOfGoodsSold = 50,
    OperatingExpense = 51,
    DepreciationExpense = 52,
    TaxExpense = 53,
    InterestExpense = 54,
    OtherExpense = 55
}

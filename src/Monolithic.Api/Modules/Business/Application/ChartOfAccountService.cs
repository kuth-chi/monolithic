using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Modules.Business.Contracts;
using Monolithic.Api.Modules.Business.Domain;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;

namespace Monolithic.Api.Modules.Business.Application;

public sealed class ChartOfAccountService(ApplicationDbContext context) : IChartOfAccountService
{
    public async Task<IReadOnlyList<ChartOfAccountDto>> GetFlatAsync(Guid businessId, CancellationToken ct = default)
    {
        var accounts = await context.ChartOfAccounts.AsNoTracking()
            .Where(a => a.BusinessId == businessId)
            .Include(a => a.ParentAccount)
            .OrderBy(a => a.AccountNumber)
            .ToListAsync(ct);
        return accounts.Select(a => MapToDto(a, [])).ToList();
    }

    public async Task<IReadOnlyList<ChartOfAccountDto>> GetTreeAsync(Guid businessId, CancellationToken ct = default)
    {
        var all = await context.ChartOfAccounts.AsNoTracking()
            .Where(a => a.BusinessId == businessId)
            .OrderBy(a => a.AccountNumber)
            .ToListAsync(ct);

        // Build in-memory tree
        var lookup = all.ToDictionary(a => a.Id);
        var roots = new List<ChartOfAccount>();
        foreach (var acct in all)
        {
            if (acct.ParentAccountId is null || !lookup.ContainsKey(acct.ParentAccountId.Value))
                roots.Add(acct);
            else
                lookup[acct.ParentAccountId.Value].ChildAccounts.Add(acct);
        }
        return roots.Select(BuildTree).ToList();
    }

    public async Task<ChartOfAccountDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var account = await context.ChartOfAccounts.AsNoTracking()
            .Where(a => a.Id == id)
            .Include(a => a.ParentAccount)
            .Include(a => a.ChildAccounts)
            .FirstOrDefaultAsync(ct);
        return account is null ? null : MapToDto(account, account.ChildAccounts.Select(c => MapToDto(c, [])).ToList());
    }

    public async Task<ChartOfAccountDto> CreateAsync(CreateChartOfAccountRequest request, CancellationToken ct = default)
    {
        var account = new ChartOfAccount
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            AccountNumber = request.AccountNumber,
            Name = request.Name,
            Description = request.Description,
            AccountType = request.AccountType,
            AccountCategory = request.AccountCategory,
            ParentAccountId = request.ParentAccountId,
            IsHeaderAccount = request.IsHeaderAccount,
            CurrencyCode = request.CurrencyCode,
            IsActive = true,
            IsSystem = false,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        context.ChartOfAccounts.Add(account);
        await context.SaveChangesAsync(ct);
        return (await GetByIdAsync(account.Id, ct))!;
    }

    public async Task<ChartOfAccountDto> UpdateAsync(Guid id, UpdateChartOfAccountRequest request, CancellationToken ct = default)
    {
        var account = await context.ChartOfAccounts.FindAsync([id], ct)
                      ?? throw new KeyNotFoundException($"ChartOfAccount {id} not found.");
        if (account.IsSystem)
            throw new InvalidOperationException("System accounts cannot be modified.");

        account.Name = request.Name;
        account.Description = request.Description;
        account.AccountCategory = request.AccountCategory;
        account.ParentAccountId = request.ParentAccountId;
        account.IsHeaderAccount = request.IsHeaderAccount;
        account.CurrencyCode = request.CurrencyCode;
        account.IsActive = request.IsActive;
        account.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(ct);
        return (await GetByIdAsync(id, ct))!;
    }

    // ── Standard COA Seeding ──────────────────────────────────────────────────
    public async Task SeedStandardCOAAsync(Guid businessId, string baseCurrencyCode, CancellationToken ct = default)
    {
        // Only seed if no accounts exist yet
        if (await context.ChartOfAccounts.AnyAsync(a => a.BusinessId == businessId, ct))
            return;

        var accounts = BuildStandardCOA(businessId, baseCurrencyCode);
        context.ChartOfAccounts.AddRange(accounts);
        await context.SaveChangesAsync(ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static ChartOfAccountDto BuildTree(ChartOfAccount node) =>
        MapToDto(node, node.ChildAccounts.Select(BuildTree).ToList());

    private static ChartOfAccountDto MapToDto(ChartOfAccount a, IReadOnlyList<ChartOfAccountDto> children) =>
        new()
        {
            Id = a.Id,
            BusinessId = a.BusinessId,
            AccountNumber = a.AccountNumber,
            Name = a.Name,
            Description = a.Description,
            AccountType = a.AccountType.ToString(),
            AccountCategory = a.AccountCategory.ToString(),
            ParentAccountId = a.ParentAccountId,
            ParentAccountName = a.ParentAccount?.Name,
            IsHeaderAccount = a.IsHeaderAccount,
            CurrencyCode = a.CurrencyCode,
            IsActive = a.IsActive,
            IsSystem = a.IsSystem,
            Children = children
        };

    private static IReadOnlyList<ChartOfAccount> BuildStandardCOA(Guid businessId, string currency)
    {
        var now = DateTimeOffset.UtcNow;
        Guid NewId() => Guid.NewGuid();

        ChartOfAccount Acct(string num, string name, AccountType type, AccountCategory cat,
            bool isHeader = false, Guid? parent = null, bool isSystem = true) =>
            new()
            {
                Id = NewId(), BusinessId = businessId, AccountNumber = num, Name = name,
                AccountType = type, AccountCategory = cat, IsHeaderAccount = isHeader,
                ParentAccountId = parent, IsSystem = isSystem, IsActive = true,
                CurrencyCode = null, CreatedAtUtc = now
            };

        // ── Assets ────────────────────────────────────────────────────────────
        var assetHeader = Acct("1000", "Assets", AccountType.Asset, AccountCategory.CurrentAsset, isHeader: true);
        var cashHeader = Acct("1100", "Cash and Cash Equivalents", AccountType.Asset, AccountCategory.CurrentAsset, isHeader: true, parent: assetHeader.Id);
        var petyCash = Acct("1110", "Petty Cash", AccountType.Asset, AccountCategory.CurrentAsset, parent: cashHeader.Id);
        var bankUsd = Acct("1120", $"Bank Account ({currency})", AccountType.Asset, AccountCategory.CurrentAsset, parent: cashHeader.Id);

        var currentAssetHeader = Acct("1200", "Current Assets", AccountType.Asset, AccountCategory.CurrentAsset, isHeader: true, parent: assetHeader.Id);
        var accountsReceivable = Acct("1210", "Accounts Receivable", AccountType.Asset, AccountCategory.CurrentAsset, parent: currentAssetHeader.Id);
        var allowanceDoubtful = Acct("1211", "Allowance for Doubtful Accounts", AccountType.Asset, AccountCategory.ContraAsset, parent: currentAssetHeader.Id);
        var inventory = Acct("1220", "Inventory", AccountType.Asset, AccountCategory.CurrentAsset, parent: currentAssetHeader.Id);
        var prepaidExpenses = Acct("1230", "Prepaid Expenses", AccountType.Asset, AccountCategory.CurrentAsset, parent: currentAssetHeader.Id);
        var vatRefundable = Acct("1240", "VAT Refundable", AccountType.Asset, AccountCategory.CurrentAsset, parent: currentAssetHeader.Id);

        var fixedAssetHeader = Acct("1500", "Fixed Assets", AccountType.Asset, AccountCategory.FixedAsset, isHeader: true, parent: assetHeader.Id);
        var equipment = Acct("1510", "Equipment", AccountType.Asset, AccountCategory.FixedAsset, parent: fixedAssetHeader.Id);
        var vehicles = Acct("1520", "Vehicles", AccountType.Asset, AccountCategory.FixedAsset, parent: fixedAssetHeader.Id);
        var accumDeprec = Acct("1590", "Accumulated Depreciation", AccountType.Asset, AccountCategory.ContraAsset, parent: fixedAssetHeader.Id);

        // ── Liabilities ───────────────────────────────────────────────────────
        var liabilityHeader = Acct("2000", "Liabilities", AccountType.Liability, AccountCategory.CurrentLiability, isHeader: true);
        var currentLiabHeader = Acct("2100", "Current Liabilities", AccountType.Liability, AccountCategory.CurrentLiability, isHeader: true, parent: liabilityHeader.Id);
        var accountsPayable = Acct("2110", "Accounts Payable", AccountType.Liability, AccountCategory.CurrentLiability, parent: currentLiabHeader.Id);
        var accruedExpenses = Acct("2120", "Accrued Expenses", AccountType.Liability, AccountCategory.CurrentLiability, parent: currentLiabHeader.Id);
        var vatPayable = Acct("2130", "VAT Payable", AccountType.Liability, AccountCategory.CurrentLiability, parent: currentLiabHeader.Id);
        var withholdingTax = Acct("2140", "Withholding Tax Payable", AccountType.Liability, AccountCategory.CurrentLiability, parent: currentLiabHeader.Id);
        var deferredRevenue = Acct("2150", "Deferred Revenue", AccountType.Liability, AccountCategory.CurrentLiability, parent: currentLiabHeader.Id);

        var longTermLiabHeader = Acct("2500", "Long-Term Liabilities", AccountType.Liability, AccountCategory.LongTermLiability, isHeader: true, parent: liabilityHeader.Id);
        var bankLoan = Acct("2510", "Bank Loan", AccountType.Liability, AccountCategory.LongTermLiability, parent: longTermLiabHeader.Id);

        // ── Equity ────────────────────────────────────────────────────────────
        var equityHeader = Acct("3000", "Equity", AccountType.Equity, AccountCategory.OwnersEquity, isHeader: true);
        var ownerCapital = Acct("3100", "Owner's Capital", AccountType.Equity, AccountCategory.OwnersEquity, parent: equityHeader.Id);
        var retainedEarnings = Acct("3200", "Retained Earnings", AccountType.Equity, AccountCategory.RetainedEarnings, parent: equityHeader.Id);

        // ── Revenue ───────────────────────────────────────────────────────────
        var revenueHeader = Acct("4000", "Revenue", AccountType.Revenue, AccountCategory.OperatingRevenue, isHeader: true);
        var salesRevenue = Acct("4100", "Sales Revenue", AccountType.Revenue, AccountCategory.OperatingRevenue, parent: revenueHeader.Id);
        var serviceRevenue = Acct("4200", "Service Revenue", AccountType.Revenue, AccountCategory.OperatingRevenue, parent: revenueHeader.Id);
        var otherRevenue = Acct("4900", "Other Revenue", AccountType.Revenue, AccountCategory.OtherRevenue, parent: revenueHeader.Id);

        // ── COGS ──────────────────────────────────────────────────────────────
        var cogsHeader = Acct("5000", "Cost of Goods Sold", AccountType.Expense, AccountCategory.CostOfGoodsSold, isHeader: true);
        var cogs = Acct("5100", "Cost of Goods Sold", AccountType.Expense, AccountCategory.CostOfGoodsSold, parent: cogsHeader.Id);
        var freightIn = Acct("5200", "Freight-In (Shipping Cost)", AccountType.Expense, AccountCategory.CostOfGoodsSold, parent: cogsHeader.Id);
        var purchaseReturns = Acct("5300", "Purchase Returns & Discounts", AccountType.Expense, AccountCategory.CostOfGoodsSold, parent: cogsHeader.Id);

        // ── Operating Expenses ────────────────────────────────────────────────
        var opexHeader = Acct("6000", "Operating Expenses", AccountType.Expense, AccountCategory.OperatingExpense, isHeader: true);
        var salaries = Acct("6100", "Salaries & Wages", AccountType.Expense, AccountCategory.OperatingExpense, parent: opexHeader.Id);
        var rent = Acct("6200", "Rent Expense", AccountType.Expense, AccountCategory.OperatingExpense, parent: opexHeader.Id);
        var utilities = Acct("6300", "Utilities Expense", AccountType.Expense, AccountCategory.OperatingExpense, parent: opexHeader.Id);
        var marketing = Acct("6400", "Marketing & Advertising", AccountType.Expense, AccountCategory.OperatingExpense, parent: opexHeader.Id);
        var officeSupplies = Acct("6500", "Office Supplies", AccountType.Expense, AccountCategory.OperatingExpense, parent: opexHeader.Id);
        var insurance = Acct("6600", "Insurance Expense", AccountType.Expense, AccountCategory.OperatingExpense, parent: opexHeader.Id);
        var depreciation = Acct("6700", "Depreciation Expense", AccountType.Expense, AccountCategory.DepreciationExpense, parent: opexHeader.Id);

        // ── Tax & Other ───────────────────────────────────────────────────────
        var taxHeader = Acct("7000", "Tax & Other Expenses", AccountType.Expense, AccountCategory.TaxExpense, isHeader: true);
        var incomeTax = Acct("7100", "Income Tax Expense", AccountType.Expense, AccountCategory.TaxExpense, parent: taxHeader.Id);
        var interestExpense = Acct("7200", "Interest Expense", AccountType.Expense, AccountCategory.InterestExpense, parent: taxHeader.Id);
        var exchangeLoss = Acct("7300", "Foreign Exchange Loss", AccountType.Expense, AccountCategory.OtherExpense, parent: taxHeader.Id);

        return [
            assetHeader, cashHeader, petyCash, bankUsd,
            currentAssetHeader, accountsReceivable, allowanceDoubtful, inventory, prepaidExpenses, vatRefundable,
            fixedAssetHeader, equipment, vehicles, accumDeprec,
            liabilityHeader, currentLiabHeader, accountsPayable, accruedExpenses, vatPayable, withholdingTax, deferredRevenue,
            longTermLiabHeader, bankLoan,
            equityHeader, ownerCapital, retainedEarnings,
            revenueHeader, salesRevenue, serviceRevenue, otherRevenue,
            cogsHeader, cogs, freightIn, purchaseReturns,
            opexHeader, salaries, rent, utilities, marketing, officeSupplies, insurance, depreciation,
            taxHeader, incomeTax, interestExpense, exchangeLoss
        ];
    }
}

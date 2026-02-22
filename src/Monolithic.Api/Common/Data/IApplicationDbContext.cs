using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Modules.Identity.Domain;
using BusinessDomain = Monolithic.Api.Modules.Business.Domain;
using FinanceDomain = Monolithic.Api.Modules.Finance.Domain;
using InventoryDomain = Monolithic.Api.Modules.Inventory.Domain;
using PurchasesDomain = Monolithic.Api.Modules.Purchases.Domain;
using SalesDomain = Monolithic.Api.Modules.Sales.Domain;

namespace Monolithic.Api.Common.Data;

/// <summary>
/// Infrastructure abstraction for the single application DbContext.
///
/// All module services depend on this interface rather than the concrete
/// <c>ApplicationDbContext</c> (which lives in Identity/Infrastructure).
/// This breaks the direct cross-module coupling and allows unit-testing
/// services without a real database.
///
/// Exposed via DI as:
///   services.AddScoped&lt;IApplicationDbContext, ApplicationDbContext&gt;()
/// in IdentityModuleRegistration.
/// </summary>
public interface IApplicationDbContext
{
    // ── Identity ──────────────────────────────────────────────────────────────
    DbSet<ApplicationUser> Users { get; }
    DbSet<ApplicationRole> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<UserPermission> UserPermissions { get; }
    DbSet<UserBusiness> UserBusinesses { get; }
    DbSet<AuthAuditLog> AuthAuditLogs { get; }
    DbSet<Contact> Contacts { get; }
    DbSet<Employee> Employees { get; }

    // ── Business ──────────────────────────────────────────────────────────────
    DbSet<BusinessDomain.Business> Businesses { get; }
    DbSet<BusinessDomain.Industry> Industries { get; }
    DbSet<BusinessDomain.BusinessIndustry> BusinessIndustries { get; }
    DbSet<BusinessDomain.BusinessContact> BusinessContacts { get; }
    DbSet<BusinessDomain.Currency> Currencies { get; }
    DbSet<BusinessDomain.ExchangeRate> ExchangeRates { get; }
    DbSet<BusinessDomain.ChartOfAccount> ChartOfAccounts { get; }
    DbSet<BusinessDomain.Vendor> Vendors { get; }
    DbSet<BusinessDomain.EstimatePurchaseOrder> EstimatePurchaseOrders { get; }
    DbSet<BusinessDomain.EstimatePurchaseOrderItem> EstimatePurchaseOrderItems { get; }
    DbSet<BusinessDomain.PurchaseOrder> PurchaseOrders { get; }
    DbSet<BusinessDomain.PurchaseOrderItem> PurchaseOrderItems { get; }
    DbSet<BusinessDomain.VendorBill> VendorBills { get; }
    DbSet<BusinessDomain.VendorBillItem> VendorBillItems { get; }
    DbSet<BusinessDomain.VendorBillPayment> VendorBillPayments { get; }
    DbSet<BusinessDomain.CostingSetup> CostingSetups { get; }
    DbSet<BusinessDomain.CostLedgerEntry> CostLedgerEntries { get; }
    DbSet<BusinessDomain.BankAccountBase> BankAccounts { get; }
    DbSet<BusinessDomain.BusinessBankAccount> BusinessBankAccounts { get; }
    DbSet<BusinessDomain.VendorBankAccount> VendorBankAccounts { get; }
    DbSet<BusinessDomain.CustomerBankAccount> CustomerBankAccounts { get; }
    DbSet<BusinessDomain.Customer> Customers { get; }
    DbSet<BusinessDomain.CustomerContact> CustomerContacts { get; }
    DbSet<BusinessDomain.CustomerAddress> CustomerAddresses { get; }
    DbSet<BusinessDomain.VendorCreditTerm> VendorCreditTerms { get; }
    DbSet<BusinessDomain.VendorClass> VendorClasses { get; }
    DbSet<BusinessDomain.VendorProfile> VendorProfiles { get; }
    DbSet<BusinessDomain.ApPaymentSession> ApPaymentSessions { get; }
    DbSet<BusinessDomain.ApPaymentSessionLine> ApPaymentSessionLines { get; }
    DbSet<BusinessDomain.ApCreditNote> ApCreditNotes { get; }
    DbSet<BusinessDomain.ApCreditNoteApplication> ApCreditNoteApplications { get; }
    DbSet<BusinessDomain.ApPaymentSchedule> ApPaymentSchedules { get; }
    DbSet<BusinessDomain.BusinessLicense> BusinessLicenses { get; }
    DbSet<BusinessDomain.BusinessOwnership> BusinessOwnerships { get; }
    DbSet<BusinessDomain.BusinessBranch> BusinessBranches { get; }
    DbSet<BusinessDomain.BranchEmployee> BranchEmployees { get; }
    DbSet<BusinessDomain.BusinessSetting> BusinessSettings { get; }
    DbSet<BusinessDomain.BusinessMedia> BusinessMedia { get; }
    DbSet<BusinessDomain.BusinessHoliday> BusinessHolidays { get; }
    DbSet<BusinessDomain.AttendancePolicy> AttendancePolicies { get; }
    DbSet<BusinessDomain.JournalEntry> JournalEntries { get; }
    DbSet<BusinessDomain.JournalEntryLine> JournalEntryLines { get; }
    DbSet<BusinessDomain.JournalEntryAuditLog> JournalEntryAuditLogs { get; }

    // ── Finance ───────────────────────────────────────────────────────────────
    DbSet<FinanceDomain.ExpenseCategory> ExpenseCategories { get; }
    DbSet<FinanceDomain.Expense> Expenses { get; }
    DbSet<FinanceDomain.ExpenseItem> ExpenseItems { get; }

    // ── Inventory ─────────────────────────────────────────────────────────────
    DbSet<InventoryDomain.InventoryItem> InventoryItems { get; }
    DbSet<InventoryDomain.InventoryTransaction> InventoryTransactions { get; }
    DbSet<InventoryDomain.Warehouse> Warehouses { get; }
    DbSet<InventoryDomain.WarehouseLocation> WarehouseLocations { get; }
    DbSet<InventoryDomain.Stock> Stocks { get; }
    DbSet<InventoryDomain.InventoryItemVariant> InventoryItemVariants { get; }
    DbSet<InventoryDomain.InventoryItemVariantAttribute> InventoryItemVariantAttributes { get; }
    DbSet<InventoryDomain.InventoryItemImage> InventoryItemImages { get; }

    // ── Sales ─────────────────────────────────────────────────────────────────
    DbSet<SalesDomain.Quotation> Quotations { get; }
    DbSet<SalesDomain.QuotationItem> QuotationItems { get; }
    DbSet<SalesDomain.SalesOrder> SalesOrders { get; }
    DbSet<SalesDomain.SalesOrderItem> SalesOrderItems { get; }
    DbSet<SalesDomain.SalesInvoice> SalesInvoices { get; }
    DbSet<SalesDomain.SalesInvoiceItem> SalesInvoiceItems { get; }
    DbSet<SalesDomain.SalesInvoicePayment> SalesInvoicePayments { get; }
    DbSet<SalesDomain.ArCreditNote> ArCreditNotes { get; }
    DbSet<SalesDomain.ArCreditNoteItem> ArCreditNoteItems { get; }
    DbSet<SalesDomain.ArCreditNoteApplication> ArCreditNoteApplications { get; }

    // ── Purchases ─────────────────────────────────────────────────────────────
    DbSet<PurchasesDomain.PurchaseReturn> PurchaseReturns { get; }
    DbSet<PurchasesDomain.PurchaseReturnItem> PurchaseReturnItems { get; }

    // ── Unit-of-Work ──────────────────────────────────────────────────────────
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

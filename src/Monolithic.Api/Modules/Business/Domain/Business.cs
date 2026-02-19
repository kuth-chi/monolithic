using Monolithic.Api.Modules.Identity.Domain;

namespace Monolithic.Api.Modules.Business.Domain;

public enum BusinessType
{
    Individual = 0,
    Business = 1
}

/// <summary>
/// Represents a business or company entity.
/// Businesses have employees, contacts, vendors, and operate in multiple industries.
/// </summary>
public class Business : BusinessPartyBase
{
    /// <summary>
    /// Short/abbreviated business name.
    /// Optional.
    /// </summary>
    public string? ShortName { get; set; }

    /// <summary>
    /// Business code for internal reference.
    /// Optional.
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Short description of the business.
    /// Optional.
    /// </summary>
    public string? ShortDescription { get; set; }

    /// <summary>
    /// Full description of the business.
    /// Optional.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Business type classification.
    /// Individual or Business.
    /// </summary>
    public BusinessType Type { get; set; } = BusinessType.Business;

    /// <summary>
    /// Local or regional name of the business.
    /// </summary>
    public string LocalName { get; set; } = string.Empty;

    /// <summary>
    /// VAT/Tax Identification Number.
    /// </summary>
    public string VatTin { get; set; } = string.Empty;

    /// <summary>
    /// Primary contact ID (user/contact that owns this business).
    /// </summary>
    public Guid? PrimaryContactId { get; set; }

    /// <summary>
    /// Business owner employee ID.
    /// </summary>
    public Guid? OwnerEmployeeId { get; set; }

    /// <summary>
    /// Navigation property to primary contact.
    /// </summary>
    public virtual Contact? PrimaryContact { get; set; }

    /// <summary>
    /// Navigation property to owner employee.
    /// </summary>
    public virtual Employee? OwnerEmployee { get; set; }

    /// <summary>
    /// Navigation property to all employees in this business.
    /// </summary>
    public virtual ICollection<Employee> Employees { get; set; } = [];

    /// <summary>
    /// Navigation property to all contacts for this business.
    /// </summary>
    public virtual ICollection<BusinessContact> BusinessContacts { get; set; } = [];

    /// <summary>
    /// Navigation property to industries this business operates in.
    /// </summary>
    public virtual ICollection<BusinessIndustry> BusinessIndustries { get; set; } = [];

    /// <summary>
    /// ISO 4217 base currency code for this business (e.g. "USD", "KHR").
    /// All amounts stored without a currency are in this currency.
    /// </summary>
    public string BaseCurrencyCode { get; set; } = "USD";

    /// <summary>
    /// Navigation property to vendors this business uses.
    /// </summary>
    public virtual ICollection<Vendor> Vendors { get; set; } = [];

    /// <summary>
    /// Navigation property to bank accounts owned by this business.
    /// </summary>
    public virtual ICollection<BusinessBankAccount> BankAccounts { get; set; } = [];

    /// <summary>Navigation to the Chart of Accounts for this business.</summary>
    public virtual ICollection<ChartOfAccount> ChartOfAccounts { get; set; } = [];

    /// <summary>Navigation to vendor bills (AP invoices) for this business.</summary>
    public virtual ICollection<VendorBill> VendorBills { get; set; } = [];

    /// <summary>Navigation to costing configurations for this business.</summary>
    public virtual ICollection<CostingSetup> CostingSetups { get; set; } = [];

    // ── Multi-branch & settings ───────────────────────────────────────────────

    /// <summary>Branches of this business (at least one: HQ).</summary>
    public virtual ICollection<BusinessBranch> Branches { get; set; } = [];

    /// <summary>Configurable settings for this business (one record).</summary>
    public virtual BusinessSetting? Settings { get; set; }

    /// <summary>Branding media (logo, cover header, favicon).</summary>
    public virtual ICollection<BusinessMedia> Media { get; set; } = [];

    /// <summary>Business-level holidays.</summary>
    public virtual ICollection<BusinessHoliday> Holidays { get; set; } = [];

    /// <summary>Attendance policies (business-wide defaults).</summary>
    public virtual ICollection<AttendancePolicy> AttendancePolicies { get; set; } = [];

    /// <summary>Ownership records (who owns this business).</summary>
    public virtual ICollection<BusinessOwnership> Ownerships { get; set; } = [];
}

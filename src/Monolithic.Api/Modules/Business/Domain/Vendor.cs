using Monolithic.Api.Common.SoftDelete;

namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Represents a vendor/supplier that supplies goods to a business.
/// Vendors are business entities from whom we purchase inventory items.
/// </summary>
public class Vendor : BusinessPartyBase, IBusinessScoped
{
    /// <summary>
    /// The business that uses this vendor.
    /// </summary>
    public Guid BusinessId { get; set; }

    /// <summary>
    /// Vendor contact person name.
    /// </summary>
    public string ContactPerson { get; set; } = string.Empty;

    /// <summary>
    /// Vendor email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Vendor phone number.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;


    /// <summary>
    /// Tax ID or vendor registration number.
    /// </summary>
    public string TaxId { get; set; } = string.Empty;

    /// <summary>
    /// Payment terms (e.g., "Net 30", "COD").
    /// </summary>
    public string PaymentTerms { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to the business.
    /// </summary>
    public virtual Business Business { get; set; } = null!;

    /// <summary>
    /// Navigation property to purchase orders from this vendor.
    /// </summary>
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = [];

    /// <summary>
    /// Navigation property to bank accounts owned by this vendor.
    /// </summary>
    public virtual ICollection<VendorBankAccount> BankAccounts { get; set; } = [];

    /// <summary>
    /// Extended AP profile: VAT %, credit terms, credit limit, classification, rating.
    /// Null until first AP profile is created for this vendor.
    /// </summary>
    public virtual VendorProfile? Profile { get; set; }

    /// <summary>Navigation to all bills for this vendor.</summary>
    public virtual ICollection<VendorBill> VendorBills { get; set; } = [];

    /// <summary>Navigation to AP credit notes.</summary>
    public virtual ICollection<ApCreditNote> CreditNotes { get; set; } = [];

    /// <summary>Navigation to pay-later schedules.</summary>
    public virtual ICollection<ApPaymentSchedule> PaymentSchedules { get; set; } = [];
}

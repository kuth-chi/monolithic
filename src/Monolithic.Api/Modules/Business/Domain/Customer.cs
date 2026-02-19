namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Represents a customer of a business.
/// A customer can have contacts, addresses, and bank accounts.
/// </summary>
public sealed class Customer : BusinessPartyBase
{
    /// <summary>The business that owns this customer record.</summary>
    public Guid BusinessId { get; set; }

    /// <summary>Customer code / reference number (e.g. "CUST-0001").</summary>
    public string CustomerCode { get; set; } = string.Empty;

    /// <summary>Primary email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Primary phone number.</summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>Tax / VAT identification number.</summary>
    public string TaxId { get; set; } = string.Empty;

    /// <summary>Payment terms agreed with the customer (e.g. "Net 30").</summary>
    public string PaymentTerms { get; set; } = string.Empty;

    /// <summary>Optional website URL.</summary>
    public string Website { get; set; } = string.Empty;

    /// <summary>Free-text notes about the customer.</summary>
    public string Notes { get; set; } = string.Empty;

    // ── Navigation ──────────────────────────────────────────────────────────

    /// <summary>The business that owns this customer.</summary>
    public Business Business { get; set; } = null!;

    /// <summary>Contact persons associated with this customer.</summary>
    public ICollection<CustomerContact> Contacts { get; set; } = [];

    /// <summary>Addresses associated with this customer.</summary>
    public ICollection<CustomerAddress> Addresses { get; set; } = [];

    /// <summary>Bank accounts owned by this customer.</summary>
    public ICollection<CustomerBankAccount> BankAccounts { get; set; } = [];
}

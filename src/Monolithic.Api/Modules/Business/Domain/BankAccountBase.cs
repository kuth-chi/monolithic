namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Shared bank account fields for DRY modeling.
/// Child classes define ownership: Business, Vendor, Customer.
/// </summary>
public abstract class BankAccountBase
{
    public Guid Id { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public string AccountNumber { get; set; } = string.Empty;

    public string BankName { get; set; } = string.Empty;

    public string BranchName { get; set; } = string.Empty;

    public string SwiftCode { get; set; } = string.Empty;

    public string RoutingNumber { get; set; } = string.Empty;

    public string CurrencyCode { get; set; } = "USD";

    public bool IsPrimary { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedAtUtc { get; set; }
}

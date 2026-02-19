using System.ComponentModel.DataAnnotations;

namespace Monolithic.Api.Modules.Finance.Contracts;

public abstract class BankAccountPayloadBase
{
    [Required]
    [MaxLength(200)]
    public string AccountName { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string AccountNumber { get; init; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string BankName { get; init; } = string.Empty;

    [MaxLength(200)]
    public string BranchName { get; init; } = string.Empty;

    [MaxLength(50)]
    public string SwiftCode { get; init; } = string.Empty;

    [MaxLength(50)]
    public string RoutingNumber { get; init; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string CurrencyCode { get; init; } = "USD";

    public bool IsPrimary { get; init; }
}

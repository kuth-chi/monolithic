using System.ComponentModel.DataAnnotations;

namespace Monolithic.Api.Modules.Sales.Customers.Contracts;

/// <summary>
/// Shared contact-person fields reused by Create and Update requests.
/// </summary>
public abstract class CustomerContactPayloadBase
{
    [Required]
    [MaxLength(200)]
    public string FullName { get; init; } = string.Empty;

    [MaxLength(120)]
    public string JobTitle { get; init; } = string.Empty;

    [MaxLength(100)]
    public string Department { get; init; } = string.Empty;

    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [MaxLength(20)]
    public string Phone { get; init; } = string.Empty;

    public bool IsPrimary { get; init; }

    public bool IsActive { get; init; } = true;
}

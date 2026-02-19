namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Customer domain is not implemented yet, so this stores CustomerId as a future-safe reference.
/// </summary>
public sealed class CustomerBankAccount : BankAccountBase
{
    public Guid CustomerId { get; set; }
}

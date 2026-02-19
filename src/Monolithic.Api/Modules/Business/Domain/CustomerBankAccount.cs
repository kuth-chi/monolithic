namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// A bank account owned by a customer.
/// </summary>
public sealed class CustomerBankAccount : BankAccountBase
{
    public Guid CustomerId { get; set; }

    public Customer Customer { get; set; } = null!;
}

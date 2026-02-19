using System.ComponentModel.DataAnnotations;

namespace Monolithic.Api.Modules.Finance.Contracts;

public sealed class CreateCustomerBankAccountRequest : BankAccountPayloadBase
{
    [Required]
    public Guid CustomerId { get; init; }
}

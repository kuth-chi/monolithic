using System.ComponentModel.DataAnnotations;

namespace Monolithic.Api.Modules.Finance.Contracts;

public sealed class CreateBusinessBankAccountRequest : BankAccountPayloadBase
{
    [Required]
    public Guid BusinessId { get; init; }
}

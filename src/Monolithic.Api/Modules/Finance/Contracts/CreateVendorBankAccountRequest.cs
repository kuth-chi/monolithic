using System.ComponentModel.DataAnnotations;

namespace Monolithic.Api.Modules.Finance.Contracts;

public sealed class CreateVendorBankAccountRequest : BankAccountPayloadBase
{
    [Required]
    public Guid VendorId { get; init; }
}

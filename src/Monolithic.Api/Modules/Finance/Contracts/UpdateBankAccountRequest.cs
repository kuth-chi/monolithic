namespace Monolithic.Api.Modules.Finance.Contracts;

public sealed class UpdateBankAccountRequest : BankAccountPayloadBase
{
    public bool IsActive { get; init; } = true;
}

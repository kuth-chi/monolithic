namespace Monolithic.Api.Modules.Business.Domain;

public sealed class BusinessBankAccount : BankAccountBase
{
    public Guid BusinessId { get; set; }

    public Business Business { get; set; } = null!;
}

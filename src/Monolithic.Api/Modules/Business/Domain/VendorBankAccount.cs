namespace Monolithic.Api.Modules.Business.Domain;

public sealed class VendorBankAccount : BankAccountBase
{
    public Guid VendorId { get; set; }

    public Vendor Vendor { get; set; } = null!;
}

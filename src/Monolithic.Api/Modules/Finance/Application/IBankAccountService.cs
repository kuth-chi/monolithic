using Monolithic.Api.Modules.Finance.Contracts;

namespace Monolithic.Api.Modules.Finance.Application;

public interface IBankAccountService
{
    Task<IReadOnlyCollection<BankAccountDto>> GetAllAsync(
        Guid? businessId = null,
        Guid? vendorId = null,
        Guid? customerId = null,
        CancellationToken cancellationToken = default);

    Task<BankAccountDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<BankAccountDto> CreateForBusinessAsync(CreateBusinessBankAccountRequest request, CancellationToken cancellationToken = default);

    Task<BankAccountDto> CreateForVendorAsync(CreateVendorBankAccountRequest request, CancellationToken cancellationToken = default);

    Task<BankAccountDto> CreateForCustomerAsync(CreateCustomerBankAccountRequest request, CancellationToken cancellationToken = default);

    Task<BankAccountDto?> UpdateAsync(Guid id, UpdateBankAccountRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

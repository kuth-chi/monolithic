using Monolithic.Api.Modules.Vendors.Contracts;

namespace Monolithic.Api.Modules.Vendors.Application;

public interface IVendorService
{
    Task<IReadOnlyCollection<VendorDto>> GetAllAsync(Guid? businessId = null, CancellationToken cancellationToken = default);

    Task<VendorDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<VendorDto> CreateAsync(CreateVendorRequest request, CancellationToken cancellationToken = default);

    Task<VendorDto?> UpdateAsync(Guid id, UpdateVendorRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

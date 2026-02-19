using Monolithic.Api.Modules.Customers.Contracts;

namespace Monolithic.Api.Modules.Customers.Application;

public interface ICustomerAddressService
{
    Task<IReadOnlyCollection<CustomerAddressDto>> GetAllAsync(Guid customerId, CancellationToken cancellationToken = default);

    Task<CustomerAddressDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CustomerAddressDto> CreateAsync(CreateCustomerAddressRequest request, CancellationToken cancellationToken = default);

    Task<CustomerAddressDto?> UpdateAsync(Guid id, UpdateCustomerAddressRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

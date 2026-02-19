using Monolithic.Api.Modules.Customers.Contracts;

namespace Monolithic.Api.Modules.Customers.Application;

public interface ICustomerService
{
    Task<IReadOnlyCollection<CustomerDto>> GetAllAsync(Guid? businessId = null, CancellationToken cancellationToken = default);

    Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);

    Task<CustomerDto?> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

using Monolithic.Api.Modules.Sales.Customers.Contracts;

namespace Monolithic.Api.Modules.Sales.Customers.Application;

public interface ICustomerService
{
    Task<IReadOnlyCollection<CustomerDto>> GetAllAsync(Guid? businessId = null, CancellationToken cancellationToken = default);

    Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);

    Task<CustomerDto?> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

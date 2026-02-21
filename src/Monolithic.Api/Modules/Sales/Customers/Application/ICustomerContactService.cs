using Monolithic.Api.Modules.Sales.Customers.Contracts;

namespace Monolithic.Api.Modules.Sales.Customers.Application;

public interface ICustomerContactService
{
    Task<IReadOnlyCollection<CustomerContactDto>> GetAllAsync(Guid customerId, CancellationToken cancellationToken = default);

    Task<CustomerContactDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CustomerContactDto> CreateAsync(CreateCustomerContactRequest request, CancellationToken cancellationToken = default);

    Task<CustomerContactDto?> UpdateAsync(Guid id, UpdateCustomerContactRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

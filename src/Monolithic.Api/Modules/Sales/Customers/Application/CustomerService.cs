using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Modules.Business.Domain;
using Monolithic.Api.Modules.Sales.Customers.Contracts;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;

namespace Monolithic.Api.Modules.Sales.Customers.Application;

public sealed class CustomerService(ApplicationDbContext context) : ICustomerService
{
    public async Task<IReadOnlyCollection<CustomerDto>> GetAllAsync(
        Guid? businessId = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Customers.AsNoTracking();

        if (businessId.HasValue)
            query = query.Where(c => c.BusinessId == businessId.Value);

        return await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => MapToDto(c))
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return customer is null ? null : MapToDto(customer);
    }

    public async Task<CustomerDto> CreateAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        var businessExists = await context.Businesses
            .AnyAsync(b => b.Id == request.BusinessId, cancellationToken);

        if (!businessExists)
            throw new InvalidOperationException("Business not found.");

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            CustomerCode = request.CustomerCode.Trim(),
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            TaxId = request.TaxId.Trim(),
            PaymentTerms = request.PaymentTerms.Trim(),
            Website = request.Website.Trim(),
            Notes = request.Notes.Trim(),
            Address = request.Address.Trim(),
            City = request.City.Trim(),
            StateProvince = request.StateProvince.Trim(),
            Country = request.Country.Trim(),
            PostalCode = request.PostalCode.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        await context.Customers.AddAsync(customer, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return MapToDto(customer);
    }

    public async Task<CustomerDto?> UpdateAsync(
        Guid id,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await context.Customers
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (customer is null)
            return null;

        customer.CustomerCode = request.CustomerCode.Trim();
        customer.Name = request.Name.Trim();
        customer.Email = request.Email.Trim();
        customer.PhoneNumber = request.PhoneNumber.Trim();
        customer.TaxId = request.TaxId.Trim();
        customer.PaymentTerms = request.PaymentTerms.Trim();
        customer.Website = request.Website.Trim();
        customer.Notes = request.Notes.Trim();
        customer.Address = request.Address.Trim();
        customer.City = request.City.Trim();
        customer.StateProvince = request.StateProvince.Trim();
        customer.Country = request.Country.Trim();
        customer.PostalCode = request.PostalCode.Trim();
        customer.IsActive = request.IsActive;
        customer.ModifiedAtUtc = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return MapToDto(customer);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await context.Customers
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (customer is null)
            return false;

        context.Customers.Remove(customer);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }

    internal static CustomerDto MapToDto(Customer c) => new()
    {
        Id = c.Id,
        BusinessId = c.BusinessId,
        CustomerCode = c.CustomerCode,
        Name = c.Name,
        Email = c.Email,
        PhoneNumber = c.PhoneNumber,
        TaxId = c.TaxId,
        PaymentTerms = c.PaymentTerms,
        Website = c.Website,
        Notes = c.Notes,
        Address = c.Address,
        City = c.City,
        StateProvince = c.StateProvince,
        Country = c.Country,
        PostalCode = c.PostalCode,
        IsActive = c.IsActive,
        CreatedAtUtc = c.CreatedAtUtc,
        ModifiedAtUtc = c.ModifiedAtUtc
    };
}

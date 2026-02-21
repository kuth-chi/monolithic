using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Modules.Business.Domain;
using Monolithic.Api.Modules.Sales.Customers.Contracts;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;

namespace Monolithic.Api.Modules.Sales.Customers.Application;

public sealed class CustomerAddressService(ApplicationDbContext context) : ICustomerAddressService
{
    public async Task<IReadOnlyCollection<CustomerAddressDto>> GetAllAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await context.CustomerAddresses
            .AsNoTracking()
            .Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAtUtc)
            .Select(a => MapToDto(a))
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerAddressDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var address = await context.CustomerAddresses
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        return address is null ? null : MapToDto(address);
    }

    public async Task<CustomerAddressDto> CreateAsync(
        CreateCustomerAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        var customerExists = await context.Customers
            .AnyAsync(c => c.Id == request.CustomerId, cancellationToken);

        if (!customerExists)
            throw new InvalidOperationException("Customer not found.");

        // If this is set as default, demote existing default addresses.
        if (request.IsDefault)
            await DemoteDefaultAddressesAsync(request.CustomerId, cancellationToken);

        var address = new CustomerAddress
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            AddressType = request.AddressType.Trim(),
            AddressLine1 = request.AddressLine1.Trim(),
            AddressLine2 = request.AddressLine2.Trim(),
            City = request.City.Trim(),
            StateProvince = request.StateProvince.Trim(),
            Country = request.Country.Trim(),
            PostalCode = request.PostalCode.Trim(),
            IsDefault = request.IsDefault,
            IsActive = request.IsActive,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        await context.CustomerAddresses.AddAsync(address, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return MapToDto(address);
    }

    public async Task<CustomerAddressDto?> UpdateAsync(
        Guid id,
        UpdateCustomerAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        var address = await context.CustomerAddresses
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (address is null)
            return null;

        // If promoting to default, demote others first.
        if (request.IsDefault && !address.IsDefault)
            await DemoteDefaultAddressesAsync(address.CustomerId, cancellationToken);

        address.AddressType = request.AddressType.Trim();
        address.AddressLine1 = request.AddressLine1.Trim();
        address.AddressLine2 = request.AddressLine2.Trim();
        address.City = request.City.Trim();
        address.StateProvince = request.StateProvince.Trim();
        address.Country = request.Country.Trim();
        address.PostalCode = request.PostalCode.Trim();
        address.IsDefault = request.IsDefault;
        address.IsActive = request.IsActive;
        address.ModifiedAtUtc = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return MapToDto(address);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var address = await context.CustomerAddresses
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (address is null)
            return false;

        context.CustomerAddresses.Remove(address);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private async Task DemoteDefaultAddressesAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var defaults = await context.CustomerAddresses
            .Where(a => a.CustomerId == customerId && a.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var a in defaults)
            a.IsDefault = false;
    }

    internal static CustomerAddressDto MapToDto(CustomerAddress a) => new()
    {
        Id = a.Id,
        CustomerId = a.CustomerId,
        AddressType = a.AddressType,
        AddressLine1 = a.AddressLine1,
        AddressLine2 = a.AddressLine2,
        City = a.City,
        StateProvince = a.StateProvince,
        Country = a.Country,
        PostalCode = a.PostalCode,
        IsDefault = a.IsDefault,
        IsActive = a.IsActive,
        CreatedAtUtc = a.CreatedAtUtc,
        ModifiedAtUtc = a.ModifiedAtUtc
    };
}

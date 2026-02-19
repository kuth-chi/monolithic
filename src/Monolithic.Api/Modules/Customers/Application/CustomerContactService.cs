using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Modules.Business.Domain;
using Monolithic.Api.Modules.Customers.Contracts;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;

namespace Monolithic.Api.Modules.Customers.Application;

public sealed class CustomerContactService(ApplicationDbContext context) : ICustomerContactService
{
    public async Task<IReadOnlyCollection<CustomerContactDto>> GetAllAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await context.CustomerContacts
            .AsNoTracking()
            .Where(cc => cc.CustomerId == customerId)
            .OrderByDescending(cc => cc.IsPrimary)
            .ThenByDescending(cc => cc.CreatedAtUtc)
            .Select(cc => MapToDto(cc))
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerContactDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contact = await context.CustomerContacts
            .AsNoTracking()
            .FirstOrDefaultAsync(cc => cc.Id == id, cancellationToken);

        return contact is null ? null : MapToDto(contact);
    }

    public async Task<CustomerContactDto> CreateAsync(
        CreateCustomerContactRequest request,
        CancellationToken cancellationToken = default)
    {
        var customerExists = await context.Customers
            .AnyAsync(c => c.Id == request.CustomerId, cancellationToken);

        if (!customerExists)
            throw new InvalidOperationException("Customer not found.");

        // If this is set as primary, demote existing primary contacts.
        if (request.IsPrimary)
            await DemotePrimaryContactsAsync(request.CustomerId, cancellationToken);

        var contact = new CustomerContact
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            FullName = request.FullName.Trim(),
            JobTitle = request.JobTitle.Trim(),
            Department = request.Department.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim(),
            IsPrimary = request.IsPrimary,
            IsActive = request.IsActive,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        await context.CustomerContacts.AddAsync(contact, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return MapToDto(contact);
    }

    public async Task<CustomerContactDto?> UpdateAsync(
        Guid id,
        UpdateCustomerContactRequest request,
        CancellationToken cancellationToken = default)
    {
        var contact = await context.CustomerContacts
            .FirstOrDefaultAsync(cc => cc.Id == id, cancellationToken);

        if (contact is null)
            return null;

        // If promoting to primary, demote others first.
        if (request.IsPrimary && !contact.IsPrimary)
            await DemotePrimaryContactsAsync(contact.CustomerId, cancellationToken);

        contact.FullName = request.FullName.Trim();
        contact.JobTitle = request.JobTitle.Trim();
        contact.Department = request.Department.Trim();
        contact.Email = request.Email.Trim();
        contact.Phone = request.Phone.Trim();
        contact.IsPrimary = request.IsPrimary;
        contact.IsActive = request.IsActive;
        contact.ModifiedAtUtc = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return MapToDto(contact);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contact = await context.CustomerContacts
            .FirstOrDefaultAsync(cc => cc.Id == id, cancellationToken);

        if (contact is null)
            return false;

        context.CustomerContacts.Remove(contact);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private async Task DemotePrimaryContactsAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var primaries = await context.CustomerContacts
            .Where(cc => cc.CustomerId == customerId && cc.IsPrimary)
            .ToListAsync(cancellationToken);

        foreach (var c in primaries)
            c.IsPrimary = false;
    }

    internal static CustomerContactDto MapToDto(CustomerContact cc) => new()
    {
        Id = cc.Id,
        CustomerId = cc.CustomerId,
        FullName = cc.FullName,
        JobTitle = cc.JobTitle,
        Department = cc.Department,
        Email = cc.Email,
        Phone = cc.Phone,
        IsPrimary = cc.IsPrimary,
        IsActive = cc.IsActive,
        CreatedAtUtc = cc.CreatedAtUtc,
        ModifiedAtUtc = cc.ModifiedAtUtc
    };
}

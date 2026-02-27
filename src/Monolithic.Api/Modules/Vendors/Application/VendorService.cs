using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Modules.Business.Domain;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;
using Monolithic.Api.Modules.Vendors.Contracts;

namespace Monolithic.Api.Modules.Vendors.Application;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Concrete implementation of <see cref="IVendorService"/>.
///
/// Vendors are domain entities that extend <see cref="BusinessPartyBase"/>,
/// giving them address, soft-delete, and IsActive behaviour out of the box
/// (DRY — no duplication with Customer or Business entities).
///
/// Finance integration:
///   - VendorBills (AP invoices) reference Vendor.Id.
///   - ApPaymentSchedules and ApCreditNotes are linked via their own FK.
///   - VendorProfile holds VAT rate, credit limit, and preferred payment method.
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class VendorService(ApplicationDbContext context) : IVendorService
{
    // ── Query ─────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyCollection<VendorDto>> GetAllAsync(
        Guid? businessId = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Vendors.AsNoTracking();

        if (businessId.HasValue)
            query = query.Where(v => v.BusinessId == businessId.Value);

        return await query
            .OrderByDescending(v => v.CreatedAtUtc)
            .Select(v => MapToDto(v))
            .ToListAsync(cancellationToken);
    }

    public async Task<VendorDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var vendor = await context.Vendors
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        return vendor is null ? null : MapToDto(vendor);
    }

    // ── Mutations ─────────────────────────────────────────────────────────────

    public async Task<VendorDto> CreateAsync(
        CreateVendorRequest request,
        CancellationToken cancellationToken = default)
    {
        var businessExists = await context.Businesses
            .AnyAsync(b => b.Id == request.BusinessId, cancellationToken);

        if (!businessExists)
            throw new InvalidOperationException($"Business '{request.BusinessId}' not found.");

        var vendor = new Vendor
        {
            Id             = Guid.NewGuid(),
            BusinessId     = request.BusinessId,
            Name           = request.Name.Trim(),
            ContactPerson  = request.ContactPerson.Trim(),
            Email          = request.Email.Trim(),
            PhoneNumber    = request.PhoneNumber.Trim(),
            Address        = request.Address.Trim(),
            City           = request.City.Trim(),
            StateProvince  = request.StateProvince.Trim(),
            Country        = request.Country.Trim(),
            PostalCode     = request.PostalCode.Trim(),
            TaxId          = request.TaxId.Trim(),
            PaymentTerms   = request.PaymentTerms.Trim(),
            IsActive       = true,
            CreatedAtUtc   = DateTimeOffset.UtcNow,
        };

        await context.Vendors.AddAsync(vendor, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return MapToDto(vendor);
    }

    public async Task<VendorDto?> UpdateAsync(
        Guid id,
        UpdateVendorRequest request,
        CancellationToken cancellationToken = default)
    {
        var vendor = await context.Vendors
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        if (vendor is null) return null;

        vendor.Name           = request.Name.Trim();
        vendor.ContactPerson  = request.ContactPerson.Trim();
        vendor.Email          = request.Email.Trim();
        vendor.PhoneNumber    = request.PhoneNumber.Trim();
        vendor.Address        = request.Address.Trim();
        vendor.City           = request.City.Trim();
        vendor.StateProvince  = request.StateProvince.Trim();
        vendor.Country        = request.Country.Trim();
        vendor.PostalCode     = request.PostalCode.Trim();
        vendor.TaxId          = request.TaxId.Trim();
        vendor.PaymentTerms   = request.PaymentTerms.Trim();
        vendor.IsActive       = request.IsActive;
        vendor.ModifiedAtUtc  = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return MapToDto(vendor);
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var vendor = await context.Vendors
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        if (vendor is null) return false;

        // Guard: prevent deletion when vendor has active purchase orders or bills
        var hasPurchaseOrders = await context.PurchaseOrders
            .AnyAsync(po => po.VendorId == id, cancellationToken);

        if (hasPurchaseOrders)
            throw new InvalidOperationException(
                "Cannot delete a vendor that is referenced by one or more purchase orders.");

        var hasVendorBills = await context.VendorBills
            .AnyAsync(vb => vb.VendorId == id, cancellationToken);

        if (hasVendorBills)
            throw new InvalidOperationException(
                "Cannot delete a vendor that is referenced by one or more vendor bills (AP invoices).");

        context.Vendors.Remove(vendor);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static VendorDto MapToDto(Vendor vendor) => new()
    {
        Id             = vendor.Id,
        BusinessId     = vendor.BusinessId,
        Name           = vendor.Name,
        ContactPerson  = vendor.ContactPerson,
        Email          = vendor.Email,
        PhoneNumber    = vendor.PhoneNumber,
        Address        = vendor.Address,
        City           = vendor.City,
        StateProvince  = vendor.StateProvince,
        Country        = vendor.Country,
        PostalCode     = vendor.PostalCode,
        TaxId          = vendor.TaxId,
        PaymentTerms   = vendor.PaymentTerms,
        IsActive       = vendor.IsActive,
        CreatedAtUtc   = vendor.CreatedAtUtc,
        ModifiedAtUtc  = vendor.ModifiedAtUtc,
    };
}

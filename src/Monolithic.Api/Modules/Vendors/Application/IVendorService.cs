using Monolithic.Api.Modules.Vendors.Contracts;

namespace Monolithic.Api.Modules.Vendors.Application;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Vendor service contract — reusable vendor/supplier CRUD usable by any module
/// that participates in procure-to-pay (Purchases, Finance, Projects, etc.).
///
/// Design principle: DIP — consumers depend on this abstraction, not
/// on any concrete storage implementation.
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public interface IVendorService
{
    /// <summary>Returns all vendors, optionally scoped to a business.</summary>
    Task<IReadOnlyCollection<VendorDto>> GetAllAsync(
        Guid? businessId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Returns a single vendor by primary key, or null if not found.</summary>
    Task<VendorDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a new vendor and returns the persisted record.</summary>
    Task<VendorDto> CreateAsync(
        CreateVendorRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing vendor's details.
    /// Returns the updated record, or null if the vendor was not found.
    /// </summary>
    Task<VendorDto?> UpdateAsync(
        Guid id,
        UpdateVendorRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes or hard-deletes a vendor.
    /// Returns false if the vendor was not found.
    /// Throws <see cref="InvalidOperationException"/> when the vendor has active POs or bills.
    /// </summary>
    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}

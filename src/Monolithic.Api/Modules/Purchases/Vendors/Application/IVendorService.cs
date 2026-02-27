// ── Backward-compatibility forwarding ─────────────────────────────────────────
// Vendor management has been extracted to its own top-level Vendors module.
// This alias keeps any internal Purchases code that references the old namespace
// compiling without changes.

namespace Monolithic.Api.Modules.Purchases.Vendors.Application;

/// <summary>
/// Forwarding interface — extends the canonical Vendors module interface.
/// New code should reference Monolithic.Api.Modules.Vendors.Application.IVendorService directly.
/// </summary>
[Obsolete("Use Monolithic.Api.Modules.Vendors.Application.IVendorService directly.")]
public interface IVendorService : Monolithic.Api.Modules.Vendors.Application.IVendorService { }

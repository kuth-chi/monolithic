// ── Deprecated ────────────────────────────────────────────────────────────────
// VendorService has been moved to the top-level Vendors module.
// Use Monolithic.Api.Modules.Vendors.Application.VendorService.
//
// This file is kept as a migration tombstone. It is safe to delete once all
// consumers have been updated to reference the new Vendors module namespace.
//
// DI wiring was removed from PurchasesModuleRegistration (see changelog).
// The IVendorService is now registered by VendorsModuleRegistration.

namespace Monolithic.Api.Modules.Purchases.Vendors.Application;

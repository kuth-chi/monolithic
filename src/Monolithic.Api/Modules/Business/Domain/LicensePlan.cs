namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// License plan tier that governs what a business owner can create.
/// Never hardcode limits in code — always read from <see cref="BusinessLicense"/>.
/// </summary>
public enum LicensePlan
{
    Free = 0,
    Starter = 1,
    Professional = 2,
    Enterprise = 3,
    Custom = 4
}

/// <summary>
/// License status lifecycle.
/// </summary>
public enum LicenseStatus
{
    Active = 0,
    Suspended = 1,
    Expired = 2,
    Cancelled = 3
}

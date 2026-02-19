using Monolithic.Api.Modules.Business.Contracts;

namespace Monolithic.Api.Modules.Business.Application;

public interface IChartOfAccountService
{
    Task<IReadOnlyList<ChartOfAccountDto>> GetTreeAsync(Guid businessId, CancellationToken ct = default);
    Task<IReadOnlyList<ChartOfAccountDto>> GetFlatAsync(Guid businessId, CancellationToken ct = default);
    Task<ChartOfAccountDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ChartOfAccountDto> CreateAsync(CreateChartOfAccountRequest request, CancellationToken ct = default);
    Task<ChartOfAccountDto> UpdateAsync(Guid id, UpdateChartOfAccountRequest request, CancellationToken ct = default);

    /// <summary>
    /// Seeds a standard Chart of Accounts for a newly created business.
    /// Call this after a business is created.
    /// </summary>
    Task SeedStandardCOAAsync(Guid businessId, string baseCurrencyCode, CancellationToken ct = default);
}

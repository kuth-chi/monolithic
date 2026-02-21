using Monolithic.Api.Modules.Platform.Themes.Contracts;
using Monolithic.Api.Modules.Platform.Themes.Domain;

namespace Monolithic.Api.Modules.Platform.Themes.Application;

public interface IThemeService
{
    Task<ThemeProfileDto?> GetDefaultAsync(Guid? businessId, CancellationToken ct = default);

    Task<IReadOnlyList<ThemeProfileDto>> ListAsync(Guid? businessId, CancellationToken ct = default);

    Task<ThemeProfileDto> UpsertAsync(UpsertThemeProfileRequest req, CancellationToken ct = default);

    Task SetDefaultAsync(Guid profileId, CancellationToken ct = default);

    Task DeleteAsync(Guid profileId, CancellationToken ct = default);
}

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

    /// <summary>
    /// Returns ShadCN/Tailwind-compatible CSS variable maps (light + dark) for
    /// the active theme of the given business.  Falls back to the system-default
    /// theme when the business has not yet configured a brand theme.
    /// All colour values are in "H S% L%" HSL channel format, safe to embed in
    /// <c>:root { --primary: &lt;value&gt;; }</c> and used with
    /// <c>hsl(var(--primary))</c>.
    /// </summary>
    Task<ShadcnCssVarsDto?> GetShadcnCssVarsAsync(Guid? businessId, CancellationToken ct = default);

    /// <summary>
    /// Downloads the logo at <paramref name="req.LogoUrl"/>, extracts dominant
    /// colours and—unless <see cref="ThemeProfile.LogoColorsOverridden"/> is set
    /// (or <c>req.ForceOverride</c> is true)—sets <c>ColorPrimary</c> and
    /// <c>ColorSecondary</c> from the top-two swatches.
    /// </summary>
    Task<ThemeProfileDto> ExtractLogoColorsAsync(
        Guid profileId, ExtractLogoColorsRequest req, CancellationToken ct = default);
}

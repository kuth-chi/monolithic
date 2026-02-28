using Monolithic.Api.Modules.Business.Domain;

namespace Monolithic.Api.Modules.Business.Contracts;

// ── GitHub License Mapping (mirrors the remote JSON schema) ──────────────────
// Schema hosted at:
//   https://raw.githubusercontent.com/ez-startup/.github/refs/heads/main/profile/license-mapping

/// <summary>Root envelope of the remote license file.</summary>
public sealed record GitHubLicenseMappingRoot(
    IReadOnlyList<GitHubLicenseEntry> Licenses);

/// <summary>
/// One licensed user entry from the remote JSON.
/// Each entry owns one email address and one or more <see cref="GitHubLicenseDetail"/>.
/// </summary>
public sealed record GitHubLicenseEntry(
    string Email,
    string FullName,
    IReadOnlyList<GitHubLicenseContact> Contacts,
    IReadOnlyList<string> BusinessIds,
    GitHubLicenseDetail License);

/// <summary>Contact person for a license entry.</summary>
public sealed record GitHubLicenseContact(
    string Name,
    string Title,
    IReadOnlyList<GitHubContactChannel> Contact);

/// <summary>A single communication channel (phone, facebook, telegram, …).</summary>
public sealed record GitHubContactChannel(
    string Kind,
    string Key);

/// <summary>
/// License quota and feature flags from the remote file.
/// Null <see cref="ExpiresOn"/> means perpetual.
/// </summary>
public sealed record GitHubLicenseDetail(
    string LicenseKey,
    string Plan,
    string Status,
    int MaxBusinesses,
    int MaxBranchesPerBusiness,
    int MaxEmployees,
    bool AllowAdvancedReporting,
    bool AllowMultiCurrency,
    bool AllowIntegrations,
    DateOnly StartsOn,
    DateOnly? ExpiresOn);

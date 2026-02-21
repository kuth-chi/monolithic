using Monolithic.Api.Modules.Platform.FeatureFlags.Domain;

namespace Monolithic.Api.Modules.Platform.FeatureFlags.Contracts;

public sealed record FeatureFlagDto(
    Guid Id,
    string Key,
    string? DisplayName,
    string? Description,
    FeatureFlagScope Scope,
    Guid? BusinessId,
    Guid? UserId,
    bool IsEnabled,
    DateTimeOffset? ExpiresAtUtc,
    string? MetadataJson,
    DateTimeOffset CreatedAtUtc
);

public sealed record UpsertFeatureFlagRequest(
    string Key,
    string? DisplayName,
    string? Description,
    FeatureFlagScope Scope,
    Guid? BusinessId,
    Guid? UserId,
    bool IsEnabled,
    DateTimeOffset? ExpiresAtUtc = null,
    string? MetadataJson = null
);

public sealed record FeatureFlagCheckResult(
    string Key,
    bool IsEnabled,
    FeatureFlagScope ResolvedScope
);

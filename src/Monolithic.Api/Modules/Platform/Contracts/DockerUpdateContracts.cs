namespace Monolithic.Api.Modules.Platform.Contracts;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Docker Update System — Contracts
///
/// Used by:
///   GET  /api/v1/admin/docker/update-check  — check all configured services
///   POST /api/v1/admin/docker/apply-update  — trigger detached pull + restart
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Status of a single container service compared against Docker Hub.
/// </summary>
public sealed record ServiceUpdateInfo(
    /// <summary>Compose service name (e.g. "api").</summary>
    string ServiceName,

    /// <summary>Running container name (e.g. "monolithic-api").</summary>
    string ContainerName,

    /// <summary>Full image name without tag (e.g. "kuthchi/monolithic-api").</summary>
    string ImageName,

    /// <summary>Image tag being tracked (e.g. "latest").</summary>
    string ImageTag,

    /// <summary>
    /// SHA-256 digest of the image currently in the local Docker daemon.
    /// Null when the local image metadata could not be read (socket unavailable).
    /// </summary>
    string? LocalDigest,

    /// <summary>
    /// SHA-256 digest of the matching tag fetched from Docker Hub.
    /// Null when Docker Hub is unreachable or the image is private without credentials.
    /// </summary>
    string? RemoteDigest,

    /// <summary>ISO-8601 — when the local image was last pulled.</summary>
    string? LocalCreatedAt,

    /// <summary>ISO-8601 — when the Docker Hub tag was last pushed.</summary>
    string? RemoteLastUpdatedAt,

    /// <summary>True when RemoteDigest != LocalDigest (or LocalDigest is null).</summary>
    bool IsUpdateAvailable,

    /// <summary>
    /// "current"          – digests match, no update needed
    /// "update_available" – newer image on Docker Hub
    /// "unknown"          – could not determine (offline / private image)
    /// "applying"         – update command has been dispatched
    /// </summary>
    string Status,

    /// <summary>Optional human-readable detail from the last check/apply.</summary>
    string? StatusDetail);

/// <summary>Aggregate result returned by GET /api/v1/admin/docker/update-check.</summary>
public sealed record DockerUpdateCheckResult(
    IReadOnlyList<ServiceUpdateInfo> Services,
    DateTimeOffset CheckedAtUtc,
    /// <summary>True when the Docker socket is accessible on the server.</summary>
    bool DockerSocketAvailable);

/// <summary>Request body for POST /api/v1/admin/docker/apply-update.</summary>
public sealed record ApplyUpdateRequest(
    /// <summary>
    /// Names of services to update. Each name must match a configured service.
    /// Send ["*"] to update all configured services.
    /// </summary>
    IReadOnlyList<string> ServiceNames);

/// <summary>Response from POST /api/v1/admin/docker/apply-update.</summary>
public sealed record ApplyUpdateResult(
    /// <summary>True when the update command was successfully dispatched.</summary>
    bool Accepted,
    string Message,
    IReadOnlyList<string> ServicesQueued);

// ── Configuration ─────────────────────────────────────────────────────────────

/// <summary>Root configuration object for the Docker Update system.</summary>
public sealed class DockerUpdateOptions
{
    public const string SectionName = "DockerUpdate";

    /// <summary>Set false to disable all endpoints (e.g. in development).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Background check interval in hours. Default 6h.</summary>
    public double CheckIntervalHours { get; set; } = 6;

    /// <summary>Path to the Docker Unix socket. Default /var/run/docker.sock.</summary>
    public string DockerSocketPath { get; set; } = "/var/run/docker.sock";

    /// <summary>Configured services to track for updates.</summary>
    public List<DockerServiceConfig> Services { get; set; } = [];
}

/// <summary>Configuration for one tracked Docker service.</summary>
public sealed class DockerServiceConfig
{
    /// <summary>Logical service name (matches docker-compose service).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Running container name. Must match <c>container_name</c> in compose file.</summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>Full Docker Hub image name without tag (namespace/repo).</summary>
    public string ImageName { get; set; } = string.Empty;

    /// <summary>Image tag to track. Default "latest".</summary>
    public string ImageTag { get; set; } = "latest";
}

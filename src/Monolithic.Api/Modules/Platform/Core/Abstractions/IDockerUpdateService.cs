using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Monolithic.Api.Modules.Platform.Contracts;

namespace Monolithic.Api.Modules.Platform.Core.Abstractions;

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IDockerUpdateService
{
    /// <summary>
    /// Checks all configured services against Docker Hub.
    /// Uses in-memory cache to avoid hammering the Docker Hub rate limit.
    /// </summary>
    Task<DockerUpdateCheckResult> CheckUpdatesAsync(CancellationToken ct = default);

    /// <summary>
    /// Dispatches a detached shell process to pull and restart the requested services.
    /// Returns immediately — the update runs in the background.
    /// The detached process outlives any container restart because it holds a
    /// reference to the host Docker socket, not the API process.
    /// </summary>
    Task<ApplyUpdateResult> ApplyUpdatesAsync(
        IReadOnlyList<string> serviceNames,
        CancellationToken ct = default);
}

// ── Implementation ────────────────────────────────────────────────────────────

/// <summary>
/// Docker Update Service
///
/// Check algorithm:
///   1. For each configured service, call Docker Hub API (HTTPS) to get the
///      remote image digest for the tracked tag.
///   2. Call the local Docker Engine API (via Unix socket) to get the digest
///      of the locally pulled image.
///   3. Compare: if digests differ → update available.
///
/// Apply algorithm (self-update safe):
///   • The update command is run as a detached nohup shell process on the host
///     (via the mounted docker socket).  A `sleep 3` gives time for the HTTP
///     response to be delivered before any container is restarted.
///   • The API container does NOT need docker-compose — only `docker` CLI.
///
/// Socket requirement: /var/run/docker.sock must be mounted into the container.
/// CLI requirement:     docker CLI must be installed in the runtime image.
/// </summary>
public sealed class DockerUpdateService(
    DockerUpdateOptions options,
    IMemoryCache cache,
    ILogger<DockerUpdateService> logger) : IDockerUpdateService
{
    private const string CheckCacheKey = "docker:update-check";
    private static readonly TimeSpan CheckCacheTtl = TimeSpan.FromMinutes(10);

    // ── Docker Hub API ────────────────────────────────────────────────────────

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
    };

    // ── CheckUpdatesAsync ─────────────────────────────────────────────────────

    public async Task<DockerUpdateCheckResult> CheckUpdatesAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(CheckCacheKey, out DockerUpdateCheckResult? cached) && cached is not null)
            return cached;

        bool socketAvailable = File.Exists(options.DockerSocketPath);
        var results          = new List<ServiceUpdateInfo>();

        foreach (var svc in options.Services)
        {
            if (ct.IsCancellationRequested) break;

            var info = await CheckServiceAsync(svc, socketAvailable, ct);
            results.Add(info);
        }

        var result = new DockerUpdateCheckResult(results, DateTimeOffset.UtcNow, socketAvailable);
        cache.Set(CheckCacheKey, result, CheckCacheTtl);
        return result;
    }

    // ── ApplyUpdatesAsync ─────────────────────────────────────────────────────

    public Task<ApplyUpdateResult> ApplyUpdatesAsync(
        IReadOnlyList<string> serviceNames,
        CancellationToken ct = default)
    {
        // Resolve wildcard
        var targets = serviceNames.Contains("*", StringComparer.OrdinalIgnoreCase)
            ? options.Services.Select(s => s.Name).ToList()
            : options.Services
                .Where(s => serviceNames.Contains(s.Name, StringComparer.OrdinalIgnoreCase))
                .Select(s => s.Name)
                .ToList();

        if (targets.Count == 0)
        {
            return Task.FromResult(new ApplyUpdateResult(
                false,
                "No matching configured services found.",
                []));
        }

        if (!File.Exists(options.DockerSocketPath))
        {
            return Task.FromResult(new ApplyUpdateResult(
                false,
                "Docker socket is not accessible. Mount /var/run/docker.sock to enable updates.",
                []));
        }

        // Build one command per service: pull image, then restart container
        var commands = new List<string>();
        foreach (var name in targets)
        {
            var svc = options.Services.First(s =>
                string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
            commands.Add($"docker pull {svc.ImageName}:{svc.ImageTag}");
            commands.Add($"docker restart {svc.ContainerName}");
        }

        // Join commands; prepend sleep so the HTTP response is sent first
        var script = "sleep 3 && " + string.Join(" && ", commands);

        logger.LogInformation("[DockerUpdate] Dispatching detached update: {Script}", script);

        try
        {
            // Run totally detached from this process so it survives container restarts
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName               = "/bin/sh",
                Arguments              = $"-c \"nohup sh -c '{script}' >/tmp/docker-update.log 2>&1 &\"",
                UseShellExecute        = false,
                RedirectStandardOutput = false,
                RedirectStandardError  = false,
                CreateNoWindow         = true,
            };
            System.Diagnostics.Process.Start(psi);

            // Invalidate check cache so next poll sees fresh state
            cache.Remove(CheckCacheKey);

            logger.LogInformation("[DockerUpdate] Update dispatched for services: {Services}", string.Join(", ", targets));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[DockerUpdate] Failed to dispatch update process.");
            return Task.FromResult(new ApplyUpdateResult(
                false,
                $"Failed to start update process: {ex.Message}",
                []));
        }

        return Task.FromResult(new ApplyUpdateResult(
            true,
            $"Update dispatched for {targets.Count} service(s). " +
            "The containers will restart in a few seconds. Refresh the page after ~30 seconds.",
            targets));
    }

    // ── Private: check one service ────────────────────────────────────────────

    private async Task<ServiceUpdateInfo> CheckServiceAsync(
        DockerServiceConfig svc,
        bool socketAvailable,
        CancellationToken ct)
    {
        string? remoteDigest     = null;
        string? remoteLastUpdate = null;
        string? localDigest      = null;
        string? localCreatedAt   = null;

        // 1. Fetch remote digest from Docker Hub
        try
        {
            (remoteDigest, remoteLastUpdate) = await FetchDockerHubDigestAsync(
                svc.ImageName, svc.ImageTag, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[DockerUpdate] Failed to fetch Docker Hub info for {Image}", svc.ImageName);
        }

        // 2. Fetch local image info via Docker Engine Unix socket
        if (socketAvailable)
        {
            try
            {
                (localDigest, localCreatedAt) = await FetchLocalImageInfoAsync(
                    $"{svc.ImageName}:{svc.ImageTag}", ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[DockerUpdate] Failed to read local image info for {Image}", svc.ImageName);
            }
        }

        // 3. Determine status
        bool updateAvailable;
        string status;
        string? detail;

        if (remoteDigest is null)
        {
            updateAvailable = false;
            status  = "unknown";
            detail  = "Could not reach Docker Hub. Check internet connectivity.";
        }
        else if (localDigest is null)
        {
            updateAvailable = true;
            status  = socketAvailable ? "update_available" : "unknown";
            detail  = socketAvailable
                ? "Local image digest unavailable — assuming update needed."
                : "Docker socket not mounted. Cannot compare local image.";
        }
        else
        {
            // Normalize digests: Docker Hub may return full manifest digest;
            // local RepoDigest is "image@sha256:...". Extract just the hash.
            var normalizedRemote = ExtractDigest(remoteDigest);
            var normalizedLocal  = ExtractDigest(localDigest);

            updateAvailable = !string.Equals(normalizedRemote, normalizedLocal, StringComparison.OrdinalIgnoreCase);
            status  = updateAvailable ? "update_available" : "current";
            detail  = updateAvailable
                ? $"New image available on Docker Hub (pushed {remoteLastUpdate})."
                : "Running the latest image.";
        }

        return new ServiceUpdateInfo(
            ServiceName:       svc.Name,
            ContainerName:     svc.ContainerName,
            ImageName:         svc.ImageName,
            ImageTag:          svc.ImageTag,
            LocalDigest:       localDigest,
            RemoteDigest:      remoteDigest,
            LocalCreatedAt:    localCreatedAt,
            RemoteLastUpdatedAt: remoteLastUpdate,
            IsUpdateAvailable: updateAvailable,
            Status:            status,
            StatusDetail:      detail);
    }

    // ── Docker Hub API ────────────────────────────────────────────────────────

    private static async Task<(string? digest, string? lastUpdated)> FetchDockerHubDigestAsync(
        string imageName, string tag, CancellationToken ct)
    {
        // Docker Hub public API — no auth required for public images
        var url = $"https://hub.docker.com/v2/repositories/{imageName}/tags/{tag}/";

        using var http = new HttpClient();
        http.Timeout = TimeSpan.FromSeconds(10);
        http.DefaultRequestHeaders.Add("User-Agent", "SMERP-DockerUpdateChecker/1.0");

        var response = await http.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode) return (null, null);

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Digest is nested: images[0].digest (architecture-specific) or top-level digest
        string? digest = null;
        if (root.TryGetProperty("digest", out var d))
            digest = d.GetString();

        if (digest is null && root.TryGetProperty("images", out var images) && images.GetArrayLength() > 0)
        {
            var firstImage = images[0];
            if (firstImage.TryGetProperty("digest", out var imgDigest))
                digest = imgDigest.GetString();
        }

        string? lastUpdated = null;
        if (root.TryGetProperty("last_updated", out var lu))
            lastUpdated = lu.GetString();

        return (digest, lastUpdated);
    }

    // ── Docker Engine API (Unix socket) ───────────────────────────────────────

    private async Task<(string? digest, string? createdAt)> FetchLocalImageInfoAsync(
        string imageRef, CancellationToken ct)
    {
        // Use UnixDomainSocket transport — no docker CLI required
        var sockHandler = new SocketsHttpHandler
        {
            ConnectCallback = async (ctx, token) =>
            {
                var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                await socket.ConnectAsync(new UnixDomainSocketEndPoint(options.DockerSocketPath), token);
                return new NetworkStream(socket, ownsSocket: true);
            },
        };

        using var dockerClient = new HttpClient(sockHandler)
        {
            BaseAddress = new Uri("http://localhost"),
            Timeout     = TimeSpan.FromSeconds(10),
        };

        // URL-encode the image reference for the Docker API
        var encodedRef = Uri.EscapeDataString(imageRef);
        var response   = await dockerClient.GetAsync($"/images/{encodedRef}/json", ct);

        if (!response.IsSuccessStatusCode)
            return (null, null);

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Extract RepoDigests — format: "kuthchi/monolithic-api@sha256:..."
        string? digest = null;
        if (root.TryGetProperty("RepoDigests", out var digests) && digests.GetArrayLength() > 0)
        {
            digest = digests[0].GetString();
        }

        string? created = null;
        if (root.TryGetProperty("Created", out var c))
            created = c.GetString();

        return (digest, created);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Extracts the raw sha256 hex from strings like:
    ///   "sha256:abc123..."
    ///   "image@sha256:abc123..."
    /// </summary>
    private static string? ExtractDigest(string? raw)
    {
        if (raw is null) return null;
        var idx = raw.IndexOf("sha256:", StringComparison.OrdinalIgnoreCase);
        return idx >= 0 ? raw[(idx + 7)..] : raw;
    }
}

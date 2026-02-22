using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Monolithic.Api.Common.Pagination;
using Monolithic.Api.Modules.Platform.Core.Abstractions;
using Monolithic.Api.Modules.Platform.Templates.Contracts;
using Monolithic.Api.Modules.Platform.Templates.Domain;

namespace Monolithic.Api.Modules.Platform.Templates.Application;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Template CRUD + versioning service.
/// Uses two-level cache (L1 memory + L2 Redis) keyed on slug+scope to avoid
/// repeated DB round-trips for hot read paths (e.g. invoice rendering).
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class TemplateService(
    IPlatformDbContext db,
    IDistributedCache cache,
    ILogger<TemplateService> logger) : ITemplateService
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    // ── List ──────────────────────────────────────────────────────────────────

    public async Task<PagedResult<TemplateDefinitionDto>> ListAsync(
        TemplateListRequest req, CancellationToken ct = default)
    {
        var query = db.TemplateDefinitions.AsNoTracking();

        if (req.Type.HasValue)         query = query.Where(t => t.Type == req.Type.Value);
        if (req.Scope.HasValue)        query = query.Where(t => t.Scope == req.Scope.Value);
        if (req.BusinessId.HasValue)   query = query.Where(t => t.BusinessId == req.BusinessId.Value);
        if (req.ActiveOnly)            query = query.Where(t => t.IsActive);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(t => t.Slug)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(t => t.ToDto())
            .ToListAsync(ct);

        return new PagedResult<TemplateDefinitionDto>
        {
            Data  = items,
            Total = total,
            Page  = req.Page,
            Size  = req.PageSize,
        };
    }

    // ── Get by ID ─────────────────────────────────────────────────────────────

    public async Task<TemplateDefinitionDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var t = await db.TemplateDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return t?.ToDto();
    }

    // ── Scope-fall-through resolution ─────────────────────────────────────────

    public async Task<TemplateDefinitionDto?> ResolveAsync(
        string slug, Guid? businessId, Guid? userId, CancellationToken ct = default)
    {
        var cacheKey = $"{PlatformConstants.TemplateCachePrefix}{slug}:{businessId}:{userId}";
        var cached = await cache.GetStringAsync(cacheKey, ct);

        if (cached is not null)
            return JsonSerializer.Deserialize<TemplateDefinitionDto>(cached, _json);

        // Try: User → Business → System
        TemplateDefinition? found = null;

        if (userId.HasValue)
            found = await db.TemplateDefinitions.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Slug == slug
                    && t.Scope == TemplateScope.User
                    && t.UserId == userId
                    && t.IsActive, ct);

        if (found is null && businessId.HasValue)
            found = await db.TemplateDefinitions.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Slug == slug
                    && t.Scope == TemplateScope.Business
                    && t.BusinessId == businessId
                    && t.IsActive, ct);

        if (found is null)
            found = await db.TemplateDefinitions.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Slug == slug
                    && t.Scope == TemplateScope.System
                    && t.IsActive, ct);

        if (found is null) return null;

        var dto = found.ToDto();
        await cache.SetStringAsync(cacheKey,
            JsonSerializer.Serialize(dto, _json),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = PlatformConstants.TemplateCacheTtl },
            ct);

        return dto;
    }

    // ── Upsert definition ─────────────────────────────────────────────────────

    public async Task<TemplateDefinitionDto> UpsertDefinitionAsync(
        UpsertTemplateDefinitionRequest req, CancellationToken ct = default)
    {
        var existing = await db.TemplateDefinitions
            .FirstOrDefaultAsync(t =>
                t.Slug == req.Slug
                && t.Scope == req.Scope
                && t.BusinessId == req.BusinessId
                && t.UserId == req.UserId, ct);

        if (existing is null)
        {
            existing = new TemplateDefinition
            {
                Id = Guid.NewGuid(),
                Slug = req.Slug,
                Type = req.Type,
                Scope = req.Scope,
                BusinessId = req.BusinessId,
                UserId = req.UserId,
            };
            db.TemplateDefinitions.Add(existing);
        }

        existing.DisplayName       = req.DisplayName;
        existing.Description       = req.Description;
        existing.AvailableVariables = req.AvailableVariables;
        existing.ModifiedAtUtc     = DateTimeOffset.UtcNow;
        existing.IsActive          = true;

        await db.SaveChangesAsync(ct);
        await InvalidateCacheAsync(existing.Slug, existing.BusinessId, existing.UserId, ct);

        return existing.ToDto();
    }

    // ── Add version ───────────────────────────────────────────────────────────

    public async Task<TemplateVersionDto> AddVersionAsync(
        CreateTemplateVersionRequest req, CancellationToken ct = default)
    {
        var definition = await db.TemplateDefinitions
            .FirstOrDefaultAsync(t => t.Id == req.TemplateDefinitionId, ct)
            ?? throw new KeyNotFoundException($"Template definition {req.TemplateDefinitionId} not found.");

        var version = new TemplateVersion
        {
            Id                   = Guid.NewGuid(),
            TemplateDefinitionId = req.TemplateDefinitionId,
            Content              = req.Content,
            Subject              = req.Subject,
            PlainTextFallback    = req.PlainTextFallback,
            VersionLabel         = req.VersionLabel,
            ChangeNotes          = req.ChangeNotes,
            CreatedByUserId      = req.CreatedByUserId,
        };

        db.TemplateVersions.Add(version);

        if (req.SetAsActive)
            definition.ActiveVersionId = version.Id;

        definition.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await InvalidateCacheAsync(definition.Slug, definition.BusinessId, definition.UserId, ct);

        return version.ToVersionDto();
    }

    // ── Activate version ──────────────────────────────────────────────────────

    public async Task ActivateVersionAsync(Guid definitionId, Guid versionId, CancellationToken ct = default)
    {
        var definition = await db.TemplateDefinitions
            .FirstOrDefaultAsync(t => t.Id == definitionId, ct)
            ?? throw new KeyNotFoundException($"Template definition {definitionId} not found.");

        var versionExists = await db.TemplateVersions
            .AnyAsync(v => v.Id == versionId && v.TemplateDefinitionId == definitionId, ct);

        if (!versionExists)
            throw new KeyNotFoundException($"Template version {versionId} not found for definition {definitionId}.");

        definition.ActiveVersionId = versionId;
        definition.ModifiedAtUtc   = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await InvalidateCacheAsync(definition.Slug, definition.BusinessId, definition.UserId, ct);
    }

    // ── Get versions ──────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<TemplateVersionDto>> GetVersionsAsync(
        Guid definitionId, CancellationToken ct = default)
    {
        return await db.TemplateVersions.AsNoTracking()
            .Where(v => v.TemplateDefinitionId == definitionId)
            .OrderByDescending(v => v.CreatedAtUtc)
            .Select(v => v.ToVersionDto())
            .ToListAsync(ct);
    }

    // ── Deactivate ────────────────────────────────────────────────────────────

    public async Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var t = await db.TemplateDefinitions.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException($"Template definition {id} not found.");
        t.IsActive       = false;
        t.ModifiedAtUtc  = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await InvalidateCacheAsync(t.Slug, t.BusinessId, t.UserId, ct);
    }

    // ── Seed defaults ─────────────────────────────────────────────────────────

    public async Task SeedDefaultsAsync(
        IEnumerable<DefaultTemplateDescriptor> descriptors, CancellationToken ct = default)
    {
        foreach (var d in descriptors)
        {
            if (await db.TemplateDefinitions.AnyAsync(
                t => t.Slug == d.Slug && t.Scope == TemplateScope.System, ct))
                continue;

            var type = Enum.Parse<TemplateType>(d.TemplateType, ignoreCase: true);

            var definition = new TemplateDefinition
            {
                Id            = Guid.NewGuid(),
                Slug          = d.Slug,
                DisplayName   = d.DisplayName,
                Type          = type,
                Scope         = TemplateScope.System,
                AvailableVariables = d.AvailableVariables,
            };

            var version = new TemplateVersion
            {
                Id                   = Guid.NewGuid(),
                TemplateDefinitionId = definition.Id,
                Content              = d.DefaultContent,
                Subject              = d.DefaultSubject,
                VersionLabel         = "1.0",
                ChangeNotes          = "Initial system default.",
                CreatedByUserId      = Guid.Empty, // system
            };

            definition.ActiveVersionId = version.Id;
            definition.Versions.Add(version);

            db.TemplateDefinitions.Add(definition);
            logger.LogInformation("[Templates] Seeded system template: {Slug}", d.Slug);
        }

        await db.SaveChangesAsync(ct);
    }

    // ── Cache helpers ─────────────────────────────────────────────────────────

    private async Task InvalidateCacheAsync(
        string slug, Guid? businessId, Guid? userId, CancellationToken ct)
    {
        // Bust all scope permutations for this slug
        string[] keys =
        [
            $"{PlatformConstants.TemplateCachePrefix}{slug}::",
            $"{PlatformConstants.TemplateCachePrefix}{slug}:{businessId}:",
            $"{PlatformConstants.TemplateCachePrefix}{slug}:{businessId}:{userId}",
        ];
        foreach (var key in keys)
            await cache.RemoveAsync(key, ct);
    }
}

// ── File-scoped mappers (DRY — reused in this file only) ─────────────────────

file static class TemplateMappers
{
    public static TemplateDefinitionDto ToDto(this TemplateDefinition t) => new(
        t.Id, t.Slug, t.DisplayName, t.Description,
        t.Type, t.Scope, t.BusinessId, t.UserId,
        t.AvailableVariables, t.ActiveVersionId,
        t.IsActive, t.CreatedAtUtc);

    public static TemplateVersionDto ToVersionDto(this TemplateVersion v) => new(
        v.Id, v.TemplateDefinitionId, v.Subject, v.Content,
        v.PlainTextFallback, v.VersionLabel, v.ChangeNotes, v.CreatedAtUtc);
}

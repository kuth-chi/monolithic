using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;
using Monolithic.Api.Modules.Platform.Templates.Contracts;
using Monolithic.Api.Modules.Platform.Templates.Domain;
using Scriban;
using Scriban.Runtime;
using Scriban.Syntax;

namespace Monolithic.Api.Modules.Platform.Templates.Application;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Renders templates using <b>Scriban</b> — a fast, sandboxed .NET template
/// engine with Liquid-compatible syntax.
///
/// Why Scriban over Razor?
///   • Designed for server-side text generation with untrusted content
///   • Supports {{ variable }}, for-loops, conditionals, date/string filters
///   • No ASP.NET runtime compilation required
///   • 10–50× faster than Razor for small templates
///
/// Security (OWASP A03 – Injection):
///   • Scriban runs in a sandboxed ScriptObject — no access to .NET types
///     or reflection by default.
///   • Only explicitly whitelisted variables and functions are available inside
///     templates.
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class ScribanTemplateRenderService(
    ApplicationDbContext db,
    ILogger<ScribanTemplateRenderService> logger) : ITemplateRenderService
{
    public async Task<RenderTemplateResult> RenderAsync(
        RenderTemplateRequest req, CancellationToken ct = default)
    {
        // --- Resolve definition + active version ---
        var slugQuery = db.TemplateDefinitions
            .Include(t => t.Versions)
            .AsNoTracking();

        // Scope fall-through: User → Business → System
        TemplateDefinition? definition = null;

        if (req.UserId.HasValue)
            definition = await slugQuery.FirstOrDefaultAsync(t =>
                t.Slug == req.Slug && t.Scope == TemplateScope.User
                && t.UserId == req.UserId && t.IsActive, ct);

        if (definition is null && req.BusinessId.HasValue)
            definition = await slugQuery.FirstOrDefaultAsync(t =>
                t.Slug == req.Slug && t.Scope == TemplateScope.Business
                && t.BusinessId == req.BusinessId && t.IsActive, ct);

        if (definition is null)
            definition = await slugQuery.FirstOrDefaultAsync(t =>
                t.Slug == req.Slug && t.Scope == TemplateScope.System && t.IsActive, ct);

        if (definition is null)
            return new RenderTemplateResult(false, null, null, null,
                $"No active template found for slug '{req.Slug}'.");

        var version = definition.ActiveVersionId.HasValue
            ? definition.Versions.FirstOrDefault(v => v.Id == definition.ActiveVersionId)
              ?? definition.Versions.MaxBy(v => v.CreatedAtUtc)
            : definition.Versions.MaxBy(v => v.CreatedAtUtc);

        if (version is null)
            return new RenderTemplateResult(false, null, null, null,
                $"Template '{req.Slug}' has no versions.");

        // --- Build Scriban context ---
        var scriptObj = new ScriptObject();
        foreach (var (key, value) in req.Variables)
        {
            // Sanitize keys: Scriban variable names must be lowercase_snake_case
            var safeKey = ToScribanKey(key);
            scriptObj.Add(safeKey, value);
        }

        var scribanCtx = new TemplateContext { StrictVariables = false };
        scribanCtx.PushGlobal(scriptObj);

        // --- Render body ---
        var (bodyOk, renderedBody, bodyError) = await RenderStringAsync(version.Content, scribanCtx);
        if (!bodyOk)
            return new RenderTemplateResult(false, null, null, null, bodyError);

        // --- Render subject (optional) ---
        string? renderedSubject = null;
        if (!string.IsNullOrWhiteSpace(version.Subject))
        {
            var (subjectOk, subjectText, subjectError) = await RenderStringAsync(version.Subject, scribanCtx);
            if (!subjectOk)
                return new RenderTemplateResult(false, null, null, null, subjectError);
            renderedSubject = subjectText;
        }

        // --- Render plain-text fallback (optional) ---
        string? renderedPlainText = null;
        if (!string.IsNullOrWhiteSpace(version.PlainTextFallback))
        {
            var (ptOk, ptText, _) = await RenderStringAsync(version.PlainTextFallback, scribanCtx);
            if (ptOk) renderedPlainText = ptText;
        }

        return new RenderTemplateResult(true, renderedSubject, renderedBody, renderedPlainText);
    }

    public async Task<string?> ValidateSyntaxAsync(string templateContent, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        try
        {
            var tpl = Template.Parse(templateContent);
            return tpl.HasErrors ? string.Join("; ", tpl.Messages.Select(m => m.Message)) : null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<(bool ok, string? text, string? error)> RenderStringAsync(
        string source, TemplateContext ctx)
    {
        await Task.CompletedTask;
        try
        {
            var tpl = Template.Parse(source);
            if (tpl.HasErrors)
                return (false, null, string.Join("; ", tpl.Messages.Select(m => m.Message)));

            var result = tpl.Render(ctx);
            return (true, result, null);
        }
        catch (ScriptRuntimeException ex)
        {
            logger.LogWarning(ex, "Scriban runtime error while rendering template.");
            return (false, null, ex.Message);
        }
    }

    private static string ToScribanKey(string key)
        => key.Replace("-", "_").Replace(" ", "_").ToLowerInvariant();
}

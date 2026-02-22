using System.Text.RegularExpressions;
using System.Web;

namespace Monolithic.Api.Common.Security;

/// <summary>
/// OWASP A03 (Injection) — centralises XSS and injection prevention.
///
/// Use this service in application-layer services to sanitise any
/// freeform user-supplied text before persisting or rendering it.
///
/// ⚠  This is a defence-in-depth measure.  Primary protection comes from:
///   1. Parameterised queries via EF Core (SQL injection)
///   2. CSP headers (reflected XSS)
///   3. FluentValidation rules (IsSafeText) at the request boundary
///
/// Registration:
///   services.AddSingleton&lt;IInputSanitizer, InputSanitizer&gt;();
/// </summary>
public interface IInputSanitizer
{
    /// <summary>HTML-encodes all potentially dangerous characters.</summary>
    string HtmlEncode(string? input);

    /// <summary>Strips all HTML tags from <paramref name="input"/> leaving plain text.</summary>
    string StripHtml(string? input);

    /// <summary>
    /// Removes characters commonly used in SQL injection attacks.
    /// Note: EF Core parameterisation is the primary SQL injection defence;
    /// this is a belt-and-suspenders filter for legacy raw-SQL queries only.
    /// </summary>
    string SanitizeSql(string? input);

    /// <summary>
    /// Normalises and trims a user-supplied string to a safe maximum length.
    /// Returns an empty string if <paramref name="input"/> is null or whitespace.
    /// </summary>
    string Normalize(string? input, int maxLength = 1000);
}

/// <inheritdoc/>
public sealed partial class InputSanitizer : IInputSanitizer
{
    // Matches any HTML tag (opening, closing, self-closing)
    [GeneratedRegex(@"<[^>]*(>|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex HtmlTagRegex();

    // Matches common SQL injection patterns
    [GeneratedRegex(
        @"(')|(\-\-)|(;)|(\bunion\b)|(\bselect\b)|(\bdrop\b)|(\binsert\b)|(\bdelete\b)|(\bupdate\b)|(\bexec\b)|(\bxp_\w+\b)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex SqlInjectionRegex();

    // Matches multiple consecutive whitespace characters
    [GeneratedRegex(@"\s{2,}", RegexOptions.Compiled)]
    private static partial Regex MultiSpaceRegex();

    /// <inheritdoc/>
    public string HtmlEncode(string? input)
        => string.IsNullOrWhiteSpace(input) ? string.Empty : HttpUtility.HtmlEncode(input);

    /// <inheritdoc/>
    public string StripHtml(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var stripped = HtmlTagRegex().Replace(input, " ");
        return MultiSpaceRegex().Replace(stripped, " ").Trim();
    }

    /// <inheritdoc/>
    public string SanitizeSql(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        return SqlInjectionRegex().Replace(input, string.Empty).Trim();
    }

    /// <inheritdoc/>
    public string Normalize(string? input, int maxLength = 1000)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var trimmed = input.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}

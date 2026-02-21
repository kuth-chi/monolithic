namespace Monolithic.Api.Common.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HMAC-SHA256 secret key (min 32 chars recommended).</summary>
    public string SecretKey { get; init; } = string.Empty;

    /// <summary>Token issuer (e.g. "monolithic-api").</summary>
    public string Issuer { get; init; } = "monolithic-api";

    /// <summary>Token audience (e.g. "monolithic-web").</summary>
    public string Audience { get; init; } = "monolithic-web";

    /// <summary>Access token lifetime in minutes (default 60).</summary>
    public int ExpiryMinutes { get; init; } = 60;
}

namespace Monolithic.Api.Common.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing a postal address.
///
/// Designed to work internationally:
///   - <see cref="CountryCode"/> uses ISO 3166-1 alpha-2 (e.g. "US", "KH", "GB")
///   - All string fields are trimmed on construction
///   - All fields except Line1 and CountryCode are optional
///
/// Persistence: map as an Owned Entity in EF Core.
/// </summary>
public sealed record Address
{
    public string Line1 { get; }
    public string? Line2 { get; }
    public string? City { get; }
    public string? State { get; }
    public string? PostalCode { get; }

    /// <summary>ISO 3166-1 alpha-2 country code, e.g. "US", "KH".</summary>
    public string CountryCode { get; }

    public Address(
        string line1,
        string countryCode,
        string? line2 = null,
        string? city = null,
        string? state = null,
        string? postalCode = null)
    {
        if (string.IsNullOrWhiteSpace(line1))
            throw new ArgumentException("Address Line1 is required.", nameof(line1));

        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Length != 2)
            throw new ArgumentException("Country code must be a 2-letter ISO 3166-1 alpha-2 code.", nameof(countryCode));

        Line1 = line1.Trim();
        Line2 = line2?.Trim();
        City = city?.Trim();
        State = state?.Trim();
        PostalCode = postalCode?.Trim();
        CountryCode = countryCode.ToUpperInvariant();
    }

    public override string ToString()
    {
        var parts = new[] { Line1, Line2, City, State, PostalCode, CountryCode }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        return string.Join(", ", parts);
    }
}

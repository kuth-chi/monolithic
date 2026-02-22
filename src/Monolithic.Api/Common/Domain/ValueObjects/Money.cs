namespace Monolithic.Api.Common.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing a monetary amount with its ISO 4217 currency code.
///
/// Rules:
///   - Amount must be ≥ 0 (use sign in context — a negative money is a design smell)
///   - CurrencyCode is normalised to upper-case on construction
///   - Arithmetic operators enforce same-currency; cross-currency conversion
///     must go through ICurrencyService
///
/// Persistence: map as an Owned Entity in EF Core:
///   builder.OwnsOne(x => x.Price, m => { m.Property(p => p.Amount).HasColumnName("price_amount"); ... });
/// </summary>
public sealed record Money
{
    public decimal Amount { get; }
    public string CurrencyCode { get; }

    public Money(decimal amount, string currencyCode)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Money amount cannot be negative.");

        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Length != 3)
            throw new ArgumentException("Currency code must be a 3-letter ISO 4217 code.", nameof(currencyCode));

        Amount = amount;
        CurrencyCode = currencyCode.ToUpperInvariant();
    }

    public static Money Zero(string currencyCode) => new(0m, currencyCode);

    /// <summary>Returns a new Money with the rounded amount using the provided decimal places.</summary>
    public Money Round(int decimalPlaces) => new(Math.Round(Amount, decimalPlaces, MidpointRounding.AwayFromZero), CurrencyCode);

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount + right.Amount, left.CurrencyCode);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        var result = left.Amount - right.Amount;
        if (result < 0) throw new InvalidOperationException("Subtraction would yield a negative Money.");
        return new Money(result, left.CurrencyCode);
    }

    public static Money operator *(Money money, decimal multiplier)
        => new(money.Amount * multiplier, money.CurrencyCode);

    public static Money operator *(decimal multiplier, Money money)
        => money * multiplier;

    public override string ToString() => $"{Amount:F2} {CurrencyCode}";

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (left.CurrencyCode != right.CurrencyCode)
            throw new InvalidOperationException(
                $"Cannot operate on Money with different currencies: {left.CurrencyCode} vs {right.CurrencyCode}.");
    }
}

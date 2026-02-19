using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Modules.Business.Contracts;
using Monolithic.Api.Modules.Business.Domain;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;

namespace Monolithic.Api.Modules.Business.Application;

public sealed class CurrencyService(ApplicationDbContext context) : ICurrencyService
{
    public async Task<IReadOnlyList<CurrencyDto>> GetAllAsync(CancellationToken ct = default)
        => await context.Currencies.AsNoTracking()
            .OrderBy(c => c.Code)
            .Select(c => new CurrencyDto(c.Code, c.Symbol, c.Name, c.DecimalPlaces, c.IsActive))
            .ToListAsync(ct);

    public async Task<CurrencyDto?> GetByCodeAsync(string code, CancellationToken ct = default)
        => await context.Currencies.AsNoTracking()
            .Where(c => c.Code == code.ToUpperInvariant())
            .Select(c => new CurrencyDto(c.Code, c.Symbol, c.Name, c.DecimalPlaces, c.IsActive))
            .FirstOrDefaultAsync(ct);

    public async Task<CurrencyDto> UpsertAsync(UpsertCurrencyRequest request, CancellationToken ct = default)
    {
        var code = request.Code.ToUpperInvariant();
        var existing = await context.Currencies.FindAsync([code], ct);
        if (existing is null)
        {
            existing = new Currency { Code = code };
            context.Currencies.Add(existing);
        }
        existing.Symbol = request.Symbol;
        existing.Name = request.Name;
        existing.DecimalPlaces = request.DecimalPlaces;
        existing.IsActive = request.IsActive;
        existing.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(ct);
        return new CurrencyDto(existing.Code, existing.Symbol, existing.Name, existing.DecimalPlaces, existing.IsActive);
    }

    // ── Exchange Rates ────────────────────────────────────────────────────────
    public async Task<IReadOnlyList<ExchangeRateDto>> GetRatesAsync(string from, string to, CancellationToken ct = default)
        => await context.ExchangeRates.AsNoTracking()
            .Where(r => r.FromCurrencyCode == from.ToUpperInvariant() && r.ToCurrencyCode == to.ToUpperInvariant())
            .OrderByDescending(r => r.EffectiveDate)
            .Select(r => new ExchangeRateDto(r.Id, r.FromCurrencyCode, r.ToCurrencyCode, r.Rate, r.EffectiveDate, r.ExpiryDate, r.Source, r.CreatedAtUtc))
            .ToListAsync(ct);

    public async Task<ExchangeRateDto> CreateRateAsync(CreateExchangeRateRequest request, Guid createdByUserId, CancellationToken ct = default)
    {
        var rate = new ExchangeRate
        {
            Id = Guid.NewGuid(),
            FromCurrencyCode = request.FromCurrencyCode.ToUpperInvariant(),
            ToCurrencyCode = request.ToCurrencyCode.ToUpperInvariant(),
            Rate = request.Rate,
            EffectiveDate = request.EffectiveDate,
            ExpiryDate = request.ExpiryDate,
            Source = request.Source,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        context.ExchangeRates.Add(rate);
        await context.SaveChangesAsync(ct);
        return new ExchangeRateDto(rate.Id, rate.FromCurrencyCode, rate.ToCurrencyCode, rate.Rate, rate.EffectiveDate, rate.ExpiryDate, rate.Source, rate.CreatedAtUtc);
    }

    public async Task<ConvertAmountResult> ConvertAsync(ConvertAmountRequest request, CancellationToken ct = default)
    {
        var asOf = request.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var from = request.FromCurrencyCode.ToUpperInvariant();
        var to = request.ToCurrencyCode.ToUpperInvariant();

        if (from == to)
            return new ConvertAmountResult(request.Amount, from, request.Amount, to, 1m, asOf);

        var rate = await context.ExchangeRates.AsNoTracking()
            .Where(r => r.FromCurrencyCode == from && r.ToCurrencyCode == to && r.EffectiveDate <= asOf)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"No exchange rate found for {from} → {to} as of {asOf}.");

        var converted = decimal.Round(request.Amount * rate.Rate, 2);
        return new ConvertAmountResult(request.Amount, from, converted, to, rate.Rate, rate.EffectiveDate);
    }
}

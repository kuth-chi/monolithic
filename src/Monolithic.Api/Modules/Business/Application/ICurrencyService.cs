using Monolithic.Api.Modules.Business.Contracts;

namespace Monolithic.Api.Modules.Business.Application;

public interface ICurrencyService
{
    Task<IReadOnlyList<CurrencyDto>> GetAllAsync(CancellationToken ct = default);
    Task<CurrencyDto?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<CurrencyDto> UpsertAsync(UpsertCurrencyRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<ExchangeRateDto>> GetRatesAsync(string from, string to, CancellationToken ct = default);
    Task<ExchangeRateDto> CreateRateAsync(CreateExchangeRateRequest request, Guid createdByUserId, CancellationToken ct = default);
    Task<ConvertAmountResult> ConvertAsync(ConvertAmountRequest request, CancellationToken ct = default);
}

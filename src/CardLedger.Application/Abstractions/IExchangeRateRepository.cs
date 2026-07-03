using CardLedger.Domain.Entities;

namespace CardLedger.Application.Abstractions;

public interface IExchangeRateRepository
{
    Task<ExchangeRate?> GetLatestRateAsync(string currencyCode, CancellationToken cancellationToken = default);

    Task<ExchangeRate?> GetMostRecentInWindowAsync(
        string currencyCode,
        DateOnly transactionDate,
        DateOnly windowStart,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(IEnumerable<ExchangeRate> rates, CancellationToken cancellationToken = default);

    Task<IReadOnlySet<string>> GetSupportedIsoCodesAsync(
        DateOnly fromDate,
        CancellationToken cancellationToken = default);
}

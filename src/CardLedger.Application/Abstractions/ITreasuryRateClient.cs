using CardLedger.Domain.Entities;

namespace CardLedger.Application.Abstractions;

public interface ITreasuryRateClient
{
    Task<IReadOnlyList<ExchangeRate>> FetchRatesAsync(
        DateOnly fromDate,
        CancellationToken cancellationToken = default);
}

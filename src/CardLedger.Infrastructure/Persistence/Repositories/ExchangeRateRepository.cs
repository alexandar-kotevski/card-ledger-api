using CardLedger.Application.Abstractions;
using CardLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CardLedger.Infrastructure.Persistence.Repositories;

internal sealed class ExchangeRateRepository : IExchangeRateRepository
{
    private readonly CardLedgerDbContext _dbContext;

    public ExchangeRateRepository(CardLedgerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ExchangeRate?> GetLatestRateAsync(
        string currencyCode,
        CancellationToken cancellationToken = default) =>
        await _dbContext.ExchangeRates
            .AsNoTracking()
            .Where(x => x.CurrencyCode == currencyCode)
            .OrderByDescending(x => x.RecordDate)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<ExchangeRate?> GetMostRecentInWindowAsync(
        string currencyCode,
        DateOnly transactionDate,
        DateOnly windowStart,
        CancellationToken cancellationToken = default) =>
        await _dbContext.ExchangeRates
            .AsNoTracking()
            .Where(x =>
                x.CurrencyCode == currencyCode &&
                x.RecordDate <= transactionDate &&
                x.RecordDate >= windowStart)
            .OrderByDescending(x => x.RecordDate)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task UpsertAsync(IEnumerable<ExchangeRate> rates, CancellationToken cancellationToken = default)
    {
        foreach (var rate in rates)
        {
            var existing = await _dbContext.ExchangeRates
                .FirstOrDefaultAsync(
                    x => x.CurrencyCode == rate.CurrencyCode && x.RecordDate == rate.RecordDate,
                    cancellationToken)
                .ConfigureAwait(false);

            if (existing is null)
            {
                await _dbContext.ExchangeRates.AddAsync(rate, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                existing.Rate = rate.Rate;
                existing.CountryCurrencyDesc = rate.CountryCurrencyDesc;
            }
        }
    }
}

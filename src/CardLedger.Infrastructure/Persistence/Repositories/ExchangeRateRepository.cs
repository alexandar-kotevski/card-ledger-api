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
            .OrderByDescending(x => x.EffectiveDate)
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
                x.EffectiveDate <= transactionDate &&
                x.EffectiveDate >= windowStart)
            .OrderByDescending(x => x.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task UpsertAsync(IEnumerable<ExchangeRate> rates, CancellationToken cancellationToken = default)
    {
        var deduplicated = rates
            .GroupBy(r => (r.CurrencyCode, r.EffectiveDate))
            .Select(g => g.Last())
            .ToList();

        foreach (var rate in deduplicated)
        {
            var existing = await _dbContext.ExchangeRates
                .FirstOrDefaultAsync(
                    x => x.CurrencyCode == rate.CurrencyCode && x.EffectiveDate == rate.EffectiveDate,
                    cancellationToken)
                .ConfigureAwait(false);

            if (existing is null)
            {
                var pending = _dbContext.ChangeTracker
                    .Entries<ExchangeRate>()
                    .FirstOrDefault(e =>
                        e.State == EntityState.Added &&
                        e.Entity.CurrencyCode == rate.CurrencyCode &&
                        e.Entity.EffectiveDate == rate.EffectiveDate)
                    ?.Entity;

                if (pending is not null)
                {
                    pending.Rate = rate.Rate;
                    pending.CountryCurrencyDesc = rate.CountryCurrencyDesc;
                    pending.RecordDate = rate.RecordDate;
                }
                else
                {
                    await _dbContext.ExchangeRates.AddAsync(rate, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                existing.Rate = rate.Rate;
                existing.CountryCurrencyDesc = rate.CountryCurrencyDesc;
                existing.RecordDate = rate.RecordDate;
            }
        }
    }

    public async Task<IReadOnlySet<string>> GetSupportedIsoCodesAsync(
        DateOnly fromDate,
        CancellationToken cancellationToken = default)
    {
        var codes = await _dbContext.ExchangeRates
            .AsNoTracking()
            .Where(x => x.EffectiveDate >= fromDate)
            .Select(x => x.CurrencyCode)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new HashSet<string>(codes, StringComparer.OrdinalIgnoreCase);
    }
}

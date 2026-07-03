using CardLedger.Application.Abstractions;
using CardLedger.Domain.Entities;
using CardLedger.Infrastructure.Persistence;
using CardLedger.Infrastructure.Treasury;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CardLedger.Integration.Tests;

internal sealed class IntegrationTestCurrencyBootstrap : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public IntegrationTestCurrencyBootstrap(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<TreasurySyncOptions>>().Value;
        if (options.Enabled)
        {
            return;
        }

        var db = scope.ServiceProvider.GetRequiredService<CardLedgerDbContext>();
        var cutoff = options.GetSupportedCurrencyFromDate();
        var seedDate = cutoff > new DateOnly(2026, 1, 1) ? cutoff : new DateOnly(2026, 1, 1);

        if (!await db.ExchangeRates.AnyAsync(x => x.CurrencyCode == "USD", cancellationToken).ConfigureAwait(false))
        {
            await db.ExchangeRates.AddAsync(
                new ExchangeRate
                {
                    CountryCurrencyDesc = "United States-Dollar",
                    CurrencyCode = "USD",
                    Rate = 1m,
                    EffectiveDate = seedDate,
                    RecordDate = seedDate
                },
                cancellationToken).ConfigureAwait(false);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        if (!await db.ExchangeRates.AnyAsync(x => x.CurrencyCode == "EUR", cancellationToken).ConfigureAwait(false))
        {
            await db.ExchangeRates.AddAsync(
                new ExchangeRate
                {
                    CountryCurrencyDesc = "Euro-Euro",
                    CurrencyCode = "EUR",
                    Rate = 0.90m,
                    EffectiveDate = seedDate,
                    RecordDate = seedDate
                },
                cancellationToken).ConfigureAwait(false);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var cache = scope.ServiceProvider.GetRequiredService<ISupportedCurrencyCache>();
        await cache.RefreshAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

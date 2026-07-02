using CardLedger.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CardLedger.Infrastructure.Treasury;

internal sealed class TreasuryRateSyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TreasurySyncOptions _options;
    private readonly TreasurySyncState _syncState;
    private readonly ILogger<TreasuryRateSyncService> _logger;

    public TreasuryRateSyncService(
        IServiceScopeFactory scopeFactory,
        IOptions<TreasurySyncOptions> options,
        TreasurySyncState syncState,
        ILogger<TreasuryRateSyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _syncState = syncState;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Treasury sync is disabled.");
            _syncState.MarkBootstrapComplete();
            await base.StartAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        _logger.LogInformation("Starting Treasury bootstrap sync (last 6 months).");
        var bootstrapFrom = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-6);
        await SyncRatesAsync(bootstrapFrom, cancellationToken).ConfigureAwait(false);
        _syncState.MarkBootstrapComplete();
        _logger.LogInformation("Treasury bootstrap sync completed.");

        await base.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextRun(DateTime.UtcNow);
            try
            {
                await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
            _logger.LogInformation("Starting Treasury incremental sync from {FromDate}.", yesterday);
            await SyncRatesAsync(yesterday, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task SyncRatesAsync(DateOnly fromDate, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<ITreasuryRateClient>();
        var repository = scope.ServiceProvider.GetRequiredService<IExchangeRateRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var rates = await client.FetchRatesAsync(fromDate, cancellationToken).ConfigureAwait(false);
        if (rates.Count == 0)
        {
            _logger.LogWarning("Treasury sync returned no rates for {FromDate}.", fromDate);
            return;
        }

        await repository.UpsertAsync(rates, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Treasury sync upserted {Count} rates from {FromDate}.", rates.Count, fromDate);
    }

    private TimeSpan GetDelayUntilNextRun(DateTime utcNow)
    {
        if (!TimeSpan.TryParse(_options.DailyRunTimeUtc, out var runTime))
        {
            runTime = TimeSpan.Zero;
        }

        var nextRun = utcNow.Date.Add(runTime);
        if (nextRun <= utcNow)
        {
            nextRun = nextRun.AddDays(1);
        }

        return nextRun - utcNow;
    }
}

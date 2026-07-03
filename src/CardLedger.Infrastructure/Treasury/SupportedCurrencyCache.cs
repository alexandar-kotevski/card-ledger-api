using CardLedger.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CardLedger.Infrastructure.Treasury;

internal sealed class SupportedCurrencyCache : ISupportedCurrencyCache
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TreasurySyncOptions _options;
    private HashSet<string> _supportedIso = new(StringComparer.OrdinalIgnoreCase);

    public SupportedCurrencyCache(
        IServiceScopeFactory scopeFactory,
        IOptions<TreasurySyncOptions> options)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
    }

    public IReadOnlySet<string> SupportedIsoCodes => _supportedIso;

    public bool IsSupportedIso(string iso4217) =>
        !string.IsNullOrWhiteSpace(iso4217) &&
        _supportedIso.Contains(iso4217.Trim().ToUpperInvariant());

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IExchangeRateRepository>();
        var fromDate = _options.GetSupportedCurrencyFromDate();
        var codes = await repository
            .GetSupportedIsoCodesAsync(fromDate, cancellationToken)
            .ConfigureAwait(false);

        _supportedIso = new HashSet<string>(codes, StringComparer.OrdinalIgnoreCase);
    }
}

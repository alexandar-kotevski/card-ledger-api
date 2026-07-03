namespace CardLedger.Application.Abstractions;

public interface ISupportedCurrencyCache
{
    IReadOnlySet<string> SupportedIsoCodes { get; }

    bool IsSupportedIso(string iso4217);

    Task RefreshAsync(CancellationToken cancellationToken = default);
}

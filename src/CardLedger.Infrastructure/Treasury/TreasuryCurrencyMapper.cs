namespace CardLedger.Infrastructure.Treasury;

internal static class TreasuryCurrencyMapper
{
    private static readonly Dictionary<string, string> IsoToTreasury = new(StringComparer.OrdinalIgnoreCase)
    {
        ["USD"] = "United States-Dollar",
        ["EUR"] = "Euro-Euro",
        ["GBP"] = "United Kingdom-Pound",
        ["AUD"] = "Australia-Dollar",
        ["CAD"] = "Canada-Dollar",
        ["JPY"] = "Japan-Yen"
    };

    private static readonly Dictionary<string, string> TreasuryToIso =
        IsoToTreasury.ToDictionary(x => x.Value, x => x.Key, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyCollection<string> SupportedTreasuryDescriptions => IsoToTreasury.Values;

    public static bool TryMapTreasuryToIso(string countryCurrencyDesc, out string currencyCode) =>
        TreasuryToIso.TryGetValue(countryCurrencyDesc, out currencyCode!);

    public static string? MapIsoToTreasury(string currencyCode) =>
        IsoToTreasury.TryGetValue(currencyCode.ToUpperInvariant(), out var desc) ? desc : null;
}

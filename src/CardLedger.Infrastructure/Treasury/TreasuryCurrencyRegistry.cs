using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using CardLedger.Application.Abstractions;

namespace CardLedger.Infrastructure.Treasury;

internal sealed class TreasuryCurrencyRegistry : ITreasuryCurrencyRegistry
{
    private readonly Dictionary<string, string> _treasuryToIso;

    public TreasuryCurrencyRegistry()
    {
        var entries = LoadEntries();
        _treasuryToIso = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            _treasuryToIso[entry.CountryCurrencyDesc] = entry.Iso4217;
        }
    }

    public bool TryMapTreasuryToIso(string countryCurrencyDesc, out string iso4217) =>
        _treasuryToIso.TryGetValue(countryCurrencyDesc, out iso4217!);

    private static IReadOnlyList<TreasuryCurrencyMapEntry> LoadEntries()
    {
        var assembly = typeof(TreasuryCurrencyRegistry).Assembly;
        const string resourceName = "CardLedger.Infrastructure.Treasury.Data.treasury-currency-map.json";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found.");

        var entries = JsonSerializer.Deserialize<List<TreasuryCurrencyMapEntry>>(stream)
            ?? throw new InvalidOperationException("Treasury currency map could not be deserialised.");

        return entries;
    }

    private sealed class TreasuryCurrencyMapEntry
    {
        [JsonPropertyName("countryCurrencyDesc")]
        public string CountryCurrencyDesc { get; set; } = string.Empty;

        [JsonPropertyName("iso4217")]
        public string Iso4217 { get; set; } = string.Empty;
    }
}

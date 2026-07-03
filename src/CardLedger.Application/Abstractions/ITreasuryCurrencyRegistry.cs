namespace CardLedger.Application.Abstractions;

public interface ITreasuryCurrencyRegistry
{
    bool TryMapTreasuryToIso(string countryCurrencyDesc, out string iso4217);
}

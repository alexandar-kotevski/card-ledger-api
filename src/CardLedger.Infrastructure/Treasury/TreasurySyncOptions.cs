namespace CardLedger.Infrastructure.Treasury;

public sealed class TreasurySyncOptions
{
    public const string SectionName = "TreasurySync";

    public bool Enabled { get; set; } = true;

    public string BaseUrl { get; set; } =
        "https://api.fiscaldata.treasury.gov/services/api/fiscal_service/v1/accounting/od/rates_of_exchange";

    public string DailyRunTimeUtc { get; set; } = "00:00:00";

    public int PageSize { get; set; } = 1000;
}

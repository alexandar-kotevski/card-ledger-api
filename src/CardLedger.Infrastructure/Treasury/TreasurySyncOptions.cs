namespace CardLedger.Infrastructure.Treasury;

public sealed class TreasurySyncOptions
{
    public const string SectionName = "TreasurySync";

    public bool Enabled { get; set; } = true;

    public string BaseUrl { get; set; } =
        "https://api.fiscaldata.treasury.gov/services/api/fiscal_service/v1/accounting/od/rates_of_exchange";

    public string DailyRunTimeUtc { get; set; } = "00:00:00";

    public int PageSize { get; set; } = 1000;

    public string SupportedCurrencyFromDate { get; set; } = "2025-12-31";

    public DateOnly GetSupportedCurrencyFromDate() =>
        DateOnly.TryParseExact(
            SupportedCurrencyFromDate,
            "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out var parsed)
            ? parsed
            : new DateOnly(2025, 12, 31);

    public DateOnly GetBootstrapFromDate(DateOnly utcToday)
    {
        var cutoff = GetSupportedCurrencyFromDate();
        var lookbackStart = utcToday.AddMonths(-6);
        return cutoff > lookbackStart ? cutoff : lookbackStart;
    }
}

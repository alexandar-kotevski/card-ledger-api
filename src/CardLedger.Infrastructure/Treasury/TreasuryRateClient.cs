using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CardLedger.Application.Abstractions;
using CardLedger.Domain.Entities;
using Microsoft.Extensions.Options;

namespace CardLedger.Infrastructure.Treasury;

internal sealed class TreasuryRateClient : ITreasuryRateClient
{
    private readonly HttpClient _httpClient;
    private readonly TreasurySyncOptions _options;
    private readonly ITreasuryCurrencyRegistry _currencyRegistry;

    public TreasuryRateClient(
        HttpClient httpClient,
        IOptions<TreasurySyncOptions> options,
        ITreasuryCurrencyRegistry currencyRegistry)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _currencyRegistry = currencyRegistry;
    }

    public async Task<IReadOnlyList<ExchangeRate>> FetchRatesAsync(
        DateOnly fromDate,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ExchangeRate>();
        var pageNumber = 1;
        var totalPages = 1;

        while (pageNumber <= totalPages)
        {
            var url =
                $"{_options.BaseUrl}?fields=country_currency_desc,exchange_rate,record_date,effective_date" +
                $"&filter=record_date:gte:{fromDate:yyyy-MM-dd}" +
                $"&sort=-record_date" +
                $"&page[size]={_options.PageSize}" +
                $"&page[number]={pageNumber}";

            var response = await _httpClient
                .GetFromJsonAsync<TreasuryRatesResponse>(url, cancellationToken)
                .ConfigureAwait(false);

            if (response?.Data is null || response.Data.Count == 0)
            {
                break;
            }

            foreach (var item in response.Data)
            {
                if (!_currencyRegistry.TryMapTreasuryToIso(item.CountryCurrencyDesc, out var currencyCode))
                {
                    continue;
                }

                if (!decimal.TryParse(
                        item.ExchangeRate,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out var rate))
                {
                    continue;
                }

                if (!DateOnly.TryParseExact(
                        item.RecordDate,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var recordDate))
                {
                    continue;
                }

                if (!DateOnly.TryParseExact(
                        item.EffectiveDate,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var effectiveDate))
                {
                    effectiveDate = recordDate;
                }

                results.Add(new ExchangeRate
                {
                    CountryCurrencyDesc = item.CountryCurrencyDesc,
                    CurrencyCode = currencyCode,
                    Rate = rate,
                    EffectiveDate = effectiveDate,
                    RecordDate = recordDate
                });
            }

            totalPages = response.Meta?.Pagination?.TotalPages ?? 1;
            pageNumber++;
        }

        return results
            .GroupBy(r => (r.CurrencyCode, r.EffectiveDate))
            .Select(g => g.Last())
            .ToList();
    }

    private sealed class TreasuryRatesResponse
    {
        [JsonPropertyName("data")]
        public List<TreasuryRateItem>? Data { get; set; }

        [JsonPropertyName("meta")]
        public TreasuryMeta? Meta { get; set; }
    }

    private sealed class TreasuryRateItem
    {
        [JsonPropertyName("country_currency_desc")]
        public string CountryCurrencyDesc { get; set; } = string.Empty;

        [JsonPropertyName("exchange_rate")]
        public string ExchangeRate { get; set; } = string.Empty;

        [JsonPropertyName("record_date")]
        public string RecordDate { get; set; } = string.Empty;

        [JsonPropertyName("effective_date")]
        public string EffectiveDate { get; set; } = string.Empty;
    }

    private sealed class TreasuryMeta
    {
        [JsonPropertyName("pagination")]
        public TreasuryPagination? Pagination { get; set; }
    }

    private sealed class TreasuryPagination
    {
        [JsonPropertyName("total-pages")]
        public int TotalPages { get; set; }
    }
}

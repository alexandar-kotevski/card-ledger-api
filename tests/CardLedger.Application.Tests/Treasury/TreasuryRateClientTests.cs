using System.Net;
using System.Text;
using CardLedger.Infrastructure.Treasury;
using Microsoft.Extensions.Options;

namespace CardLedger.Application.Tests.Treasury;

public class TreasuryRateClientTests
{
    [Fact]
    public async Task FetchRatesAsync_UsesEffectiveDateFilterInRequestUrl()
    {
        var handler = new CapturingHttpMessageHandler(_ => EmptyRatesResponse());
        var client = CreateClient(handler, new TreasurySyncOptions());
        var fromDate = new DateOnly(2026, 1, 3);

        await client.FetchRatesAsync(fromDate);

        Assert.NotNull(handler.LastRequest);
        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Contains("filter=effective_date:gte:2026-01-03", query, StringComparison.Ordinal);
        Assert.Contains("sort=-effective_date", query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FetchRatesAsync_ParsesBgnRateFromEffectiveDate()
    {
        const string treasuryJson =
            """
            {
              "data": [
                {
                  "country_currency_desc": "Bulgaria-Lev New",
                  "exchange_rate": "0.86",
                  "effective_date": "2026-01-15"
                }
              ],
              "meta": {
                "pagination": {
                  "total-pages": 1
                }
              }
            }
            """;

        var handler = new CapturingHttpMessageHandler(_ => JsonResponse(treasuryJson));
        var client = CreateClient(handler, new TreasurySyncOptions());

        var rates = await client.FetchRatesAsync(new DateOnly(2026, 1, 3));

        var bgn = Assert.Single(rates);
        Assert.Equal("BGN", bgn.CurrencyCode);
        Assert.Equal("Bulgaria-Lev New", bgn.CountryCurrencyDesc);
        Assert.Equal(0.86m, bgn.Rate);
        Assert.Equal(new DateOnly(2026, 1, 15), bgn.EffectiveDate);
    }

    private static TreasuryRateClient CreateClient(
        CapturingHttpMessageHandler handler,
        TreasurySyncOptions options)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.test/")
        };

        return new TreasuryRateClient(
            httpClient,
            Options.Create(options),
            new TreasuryCurrencyRegistry());
    }

    private static HttpResponseMessage EmptyRatesResponse() =>
        JsonResponse(
            """
            {
              "data": [],
              "meta": {
                "pagination": {
                  "total-pages": 0
                }
              }
            }
            """);

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public CapturingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_responder(request));
        }
    }
}

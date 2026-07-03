using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using Testcontainers.PostgreSql;

namespace CardLedger.Integration.Tests;

public sealed class CardLedgerFlowTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:18")
        .WithDatabase("cardledger")
        .WithUsername("cardledger")
        .WithPassword("cardledger")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
        .Build();

    private bool _dockerAvailable;
    private CardLedgerWebApplicationFactory? _factory;
    private HttpClient? _client;

    public async Task InitializeAsync()
    {
        try
        {
            await _postgres.StartAsync();
            _dockerAvailable = true;
            _factory = new CardLedgerWebApplicationFactory(_postgres.GetConnectionString());
            _client = _factory.CreateClient();
        }
        catch (Exception)
        {
            _dockerAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        if (_dockerAvailable)
        {
            await _postgres.DisposeAsync();
        }
    }

    [Fact]
    public async Task IssuePurchaseBalanceFlow_WorksEndToEnd()
    {
        if (!_dockerAvailable || _client is null)
        {
            return;
        }

        var issueResponse = await _client.PostAsJsonAsync("/api/cards", new
        {
            creditLimit = "1000.00",
            currency = "USD"
        });

        Assert.Equal(HttpStatusCode.Created, issueResponse.StatusCode);
        var issueBody = await issueResponse.Content.ReadFromJsonAsync<IssueCardPayload>(JsonOptions);
        Assert.NotNull(issueBody);

        var purchaseResponse = await _client.PostAsJsonAsync("/api/cards/transactions", new
        {
            cardNumber = issueBody!.CardNumber,
            expiryDate = issueBody.ExpiryDate,
            cvv = issueBody.Cvv,
            amount = "100.00",
            currency = "USD",
            description = "Integration test purchase"
        });

        Assert.Equal(HttpStatusCode.Created, purchaseResponse.StatusCode);

        var balanceResponse = await _client.GetAsync(
            $"/api/cards/{issueBody.CardNumber}/balance");

        Assert.Equal(HttpStatusCode.OK, balanceResponse.StatusCode);
        var balanceBody = await balanceResponse.Content.ReadFromJsonAsync<BalancePayload>(JsonOptions);
        Assert.NotNull(balanceBody);
        Assert.Equal("USD", balanceBody!.Currency);
        Assert.Equal(900m, ParseDecimal(balanceBody.AvailableBalance));
        Assert.Null(balanceBody.ConvertedBalance);
        Assert.Null(balanceBody.ConvertedCurrency);

        var fxBalanceResponse = await _client.GetAsync(
            $"/api/cards/{issueBody.CardNumber}/balance?targetCurrency=EUR");

        Assert.Equal(HttpStatusCode.OK, fxBalanceResponse.StatusCode);
        var fxBalanceBody = await fxBalanceResponse.Content.ReadFromJsonAsync<BalancePayload>(JsonOptions);
        Assert.NotNull(fxBalanceBody);
        Assert.Equal("USD", fxBalanceBody!.Currency);
        Assert.Equal(900m, ParseDecimal(fxBalanceBody.AvailableBalance));
        Assert.Equal(810m, ParseDecimal(fxBalanceBody.ConvertedBalance!));
        Assert.Equal("EUR", fxBalanceBody.ConvertedCurrency);
        Assert.NotNull(fxBalanceBody.RateUsed);
        Assert.NotNull(fxBalanceBody.RateDate);

        var bgnBalanceResponse = await _client.GetAsync(
            $"/api/cards/{issueBody.CardNumber}/balance?targetCurrency=BGN");

        Assert.Equal(HttpStatusCode.OK, bgnBalanceResponse.StatusCode);
        var bgnBalanceBody = await bgnBalanceResponse.Content.ReadFromJsonAsync<BalancePayload>(JsonOptions);
        Assert.NotNull(bgnBalanceBody);
        Assert.Equal("USD", bgnBalanceBody!.Currency);
        Assert.Equal(900m, ParseDecimal(bgnBalanceBody.AvailableBalance));
        Assert.Equal(774m, ParseDecimal(bgnBalanceBody.ConvertedBalance!));
        Assert.Equal("BGN", bgnBalanceBody.ConvertedCurrency);

        var audPurchaseResponse = await _client.PostAsJsonAsync("/api/cards/transactions", new
        {
            cardNumber = issueBody.CardNumber,
            expiryDate = issueBody.ExpiryDate,
            cvv = issueBody.Cvv,
            amount = "50.00",
            currency = "AUD",
            description = "Integration test AUD purchase"
        });

        Assert.Equal(HttpStatusCode.Created, audPurchaseResponse.StatusCode);

        var usdTxListResponse = await _client.GetAsync(
            $"/api/cards/{issueBody.CardNumber}/transactions?targetCurrency=USD");

        Assert.Equal(HttpStatusCode.OK, usdTxListResponse.StatusCode);
        var usdTxList = await usdTxListResponse.Content.ReadFromJsonAsync<List<TransactionPayload>>(JsonOptions);
        Assert.NotNull(usdTxList);
        var usdPurchase = Assert.Single(usdTxList!, tx => tx.Currency == "USD");
        Assert.Equal(100m, ParseDecimal(usdPurchase.Amount));
        Assert.Null(usdPurchase.ConvertedAmount);
        Assert.Null(usdPurchase.RateUsed);

        var bgnTxListResponse = await _client.GetAsync(
            $"/api/cards/{issueBody.CardNumber}/transactions?targetCurrency=BGN");

        Assert.Equal(HttpStatusCode.OK, bgnTxListResponse.StatusCode);
        var bgnTxList = await bgnTxListResponse.Content.ReadFromJsonAsync<List<TransactionPayload>>(JsonOptions);
        Assert.NotNull(bgnTxList);
        Assert.Equal(2, bgnTxList!.Count);

        var usdToBgn = Assert.Single(bgnTxList, tx => tx.Currency == "USD");
        Assert.Equal(100m, ParseDecimal(usdToBgn.Amount));
        Assert.Equal(86m, ParseDecimal(usdToBgn.ConvertedAmount!));
        Assert.Equal("BGN", usdToBgn.ConvertedCurrency);
        Assert.Equal("0.86", usdToBgn.RateUsed);
        Assert.Equal(new DateOnly(2026, 1, 15), usdToBgn.RateDate);

        var audToBgn = Assert.Single(bgnTxList, tx => tx.Currency == "AUD");
        Assert.Equal(50m, ParseDecimal(audToBgn.Amount));
        Assert.Equal(29.6143m, ParseDecimal(audToBgn.ConvertedAmount!));
        Assert.Equal("BGN", audToBgn.ConvertedCurrency);
        Assert.Equal("0.592286", audToBgn.RateUsed);
        Assert.Equal(new DateOnly(2026, 1, 15), audToBgn.RateDate);
    }

    private static decimal ParseDecimal(string value) =>
        decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);

    private sealed record IssueCardPayload(
        string CardNumber,
        string ExpiryDate,
        string Cvv,
        string Currency,
        string CreditLimit);

    private sealed record BalancePayload(
        string AvailableBalance,
        string Currency,
        string? ConvertedBalance = null,
        string? ConvertedCurrency = null,
        string? RateUsed = null,
        DateOnly? RateDate = null);

    private sealed record TransactionPayload(
        Guid Id,
        string Description,
        DateTimeOffset TransactionDate,
        string Amount,
        string Currency,
        string? ConvertedAmount = null,
        string? ConvertedCurrency = null,
        string? RateUsed = null,
        DateOnly? RateDate = null);
}

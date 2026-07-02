using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using Testcontainers.PostgreSql;

namespace CardLedger.Integration.Tests;

public sealed class ExchangeRateFailureTests : IAsyncLifetime
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
    public async Task GetTransactionWithTargetCurrency_Returns422WhenRateMissing()
    {
        if (!_dockerAvailable || _client is null)
        {
            return;
        }

        var issueResponse = await _client.PostAsJsonAsync("/api/cards", new
        {
            creditLimit = "500.00",
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
            amount = "50.00",
            currency = "USD",
            description = "Rate failure test purchase"
        });

        Assert.Equal(HttpStatusCode.Created, purchaseResponse.StatusCode);
        var purchaseBody = await purchaseResponse.Content.ReadFromJsonAsync<PurchasePayload>(JsonOptions);
        Assert.NotNull(purchaseBody);

        var transactionResponse = await _client.GetAsync(
            $"/api/cards/{issueBody.CardNumber}/transactions/{purchaseBody!.Id}?targetCurrency=EUR");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, transactionResponse.StatusCode);

        var problemJson = await transactionResponse.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(problemJson);
        var root = document.RootElement;

        Assert.Equal("Exchange rate not found", root.GetProperty("title").GetString());
        Assert.Equal("EUR", root.GetProperty("targetCurrency").GetString());
    }

    private sealed record IssueCardPayload(
        string CardNumber,
        string ExpiryDate,
        string Cvv,
        string Currency,
        string CreditLimit);

    private sealed record PurchasePayload(
        Guid Id,
        string Amount,
        string Currency,
        string Description);
}

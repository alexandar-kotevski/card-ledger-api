using CardLedger.Application.Abstractions;
using CardLedger.Application.Services;
using CardLedger.Domain.Entities;
using NSubstitute;
using ExchangeRateEntity = CardLedger.Domain.Entities.ExchangeRate;

namespace CardLedger.Application.Tests;

public class BalanceServiceTests
{
    private readonly ICardRepository _cardRepository = Substitute.For<ICardRepository>();
    private readonly ILedgerRepository _ledgerRepository = Substitute.For<ILedgerRepository>();
    private readonly IExchangeRateRepository _exchangeRateRepository = Substitute.For<IExchangeRateRepository>();
    private readonly BalanceService _sut;

    public BalanceServiceTests()
    {
        var lookback = new ExchangeRateLookbackService(_exchangeRateRepository);
        _sut = new BalanceService(_cardRepository, _ledgerRepository, lookback);
    }

    [Fact]
    public async Task GetBalanceAsync_ReturnsLedgerCurrencyWhenTargetOmitted()
    {
        const string pan = "4111111111111111";
        SeedCardAndLedger(pan, 750m, "USD");

        var result = await _sut.GetBalanceAsync(pan, null);

        Assert.Equal(750m, result.AvailableBalance);
        Assert.Equal("USD", result.Currency);
        Assert.Null(result.ConvertedBalance);
        Assert.Null(result.ConvertedCurrency);
        Assert.Null(result.RateUsed);
        Assert.Null(result.RateDate);
        await _exchangeRateRepository.DidNotReceiveWithAnyArgs().GetLatestRateAsync(default!, default);
    }

    [Fact]
    public async Task GetBalanceAsync_ReturnsLedgerAndConvertedWhenTargetCurrencyProvided()
    {
        const string pan = "4111111111111111";
        SeedCardAndLedger(pan, 750m, "USD");

        _exchangeRateRepository.GetLatestRateAsync("EUR", Arg.Any<CancellationToken>())
            .Returns(new ExchangeRateEntity
            {
                CurrencyCode = "EUR",
                CountryCurrencyDesc = "Euro-Euro",
                Rate = 0.90m,
                EffectiveDate = new DateOnly(2026, 6, 30),
                RecordDate = new DateOnly(2026, 6, 30)
            });

        var result = await _sut.GetBalanceAsync(pan, "EUR");

        Assert.Equal(750m, result.AvailableBalance);
        Assert.Equal("USD", result.Currency);
        Assert.Equal(675m, result.ConvertedBalance);
        Assert.Equal("EUR", result.ConvertedCurrency);
        Assert.Equal(0.90m, result.RateUsed);
        Assert.Equal(new DateOnly(2026, 6, 30), result.RateDate);
    }

    [Fact]
    public async Task GetBalanceAsync_OmitsConvertedWhenTargetMatchesLedger()
    {
        const string pan = "4111111111111111";
        SeedCardAndLedger(pan, 750m, "USD");

        var result = await _sut.GetBalanceAsync(pan, "USD");

        Assert.Equal(750m, result.AvailableBalance);
        Assert.Equal("USD", result.Currency);
        Assert.Null(result.ConvertedBalance);
        Assert.Null(result.ConvertedCurrency);
        Assert.Null(result.RateUsed);
        Assert.Null(result.RateDate);
        await _exchangeRateRepository.DidNotReceiveWithAnyArgs().GetLatestRateAsync(default!, default);
    }

    private void SeedCardAndLedger(string pan, decimal availableBalance, string currency)
    {
        var cardId = Guid.NewGuid();
        _cardRepository.GetByPanAsync(pan, Arg.Any<CancellationToken>()).Returns(new Card
        {
            Id = cardId,
            Pan = pan,
            ExpiryDate = new DateOnly(2029, 7, 31),
            CvvHash = "hash",
            CreditLimit = 1000m,
            Currency = currency,
            IssuedAt = DateTimeOffset.UtcNow
        });
        _ledgerRepository.GetByCardIdAsync(cardId, Arg.Any<CancellationToken>()).Returns(new Ledger
        {
            Id = Guid.NewGuid(),
            CardId = cardId,
            AvailableBalance = availableBalance,
            Currency = currency,
            UpdatedAt = DateTimeOffset.UtcNow
        });
    }
}

using CardLedger.Application.Abstractions;
using CardLedger.Application.Services;
using CardLedger.Domain.Entities;
using CardLedger.Domain.Exceptions;
using NSubstitute;

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
        var cardId = Guid.NewGuid();
        _cardRepository.GetByPanAsync(pan, Arg.Any<CancellationToken>()).Returns(new Card
        {
            Id = cardId,
            Pan = pan,
            ExpiryDate = new DateOnly(2029, 7, 31),
            CvvHash = "hash",
            CreditLimit = 1000m,
            Currency = "USD",
            IssuedAt = DateTimeOffset.UtcNow
        });
        _ledgerRepository.GetByCardIdAsync(cardId, Arg.Any<CancellationToken>()).Returns(new Ledger
        {
            Id = Guid.NewGuid(),
            CardId = cardId,
            AvailableBalance = 750m,
            Currency = "USD",
            UpdatedAt = DateTimeOffset.UtcNow
        });

        var result = await _sut.GetBalanceAsync(pan, null);

        Assert.Equal(750m, result.AvailableBalance);
        Assert.Equal("USD", result.Currency);
        Assert.Null(result.RateUsed);
        await _exchangeRateRepository.DidNotReceiveWithAnyArgs().GetLatestRateAsync(default!, default);
    }
}

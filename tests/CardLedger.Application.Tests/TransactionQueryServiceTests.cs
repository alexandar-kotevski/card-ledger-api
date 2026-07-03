using CardLedger.Application.Abstractions;
using CardLedger.Application.Services;
using CardLedger.Domain.Entities;
using CardLedger.Domain.Exceptions;
using NSubstitute;

namespace CardLedger.Application.Tests;

public class TransactionQueryServiceTests
{
    private readonly ICardRepository _cardRepository = Substitute.For<ICardRepository>();
    private readonly ITransactionRepository _transactionRepository = Substitute.For<ITransactionRepository>();
    private readonly IExchangeRateRepository _exchangeRateRepository = Substitute.For<IExchangeRateRepository>();
    private readonly TransactionQueryService _sut;

    public TransactionQueryServiceTests()
    {
        var lookback = new ExchangeRateLookbackService(_exchangeRateRepository);
        _sut = new TransactionQueryService(_cardRepository, _transactionRepository, lookback);
    }

    [Fact]
    public async Task GetByIdAsync_OmitsConvertedWhenTargetMatchesTransactionCurrency()
    {
        const string pan = "4111111111111111";
        var cardId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
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
        _transactionRepository
            .GetByCardPanAndIdAsync(pan, transactionId, Arg.Any<CancellationToken>())
            .Returns(new Transaction
            {
                Id = transactionId,
                CardId = cardId,
                Amount = 100m,
                Currency = "USD",
                Description = "Test",
                TransactionDate = DateTimeOffset.UtcNow
            });

        var result = await _sut.GetByIdAsync(pan, transactionId, "USD");

        Assert.Equal(100m, result.Amount);
        Assert.Equal("USD", result.Currency);
        Assert.Null(result.ConvertedAmount);
        Assert.Null(result.ConvertedCurrency);
        await _exchangeRateRepository.DidNotReceiveWithAnyArgs()
            .GetMostRecentInWindowAsync(default!, default, default, default);
    }
}

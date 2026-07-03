using CardLedger.Application.Abstractions;
using CardLedger.Application.DTOs;
using CardLedger.Application.Services;
using CardLedger.Domain.Entities;
using CardLedger.Domain.Exceptions;
using CardLedger.Domain.Services;
using CardLedger.Domain.ValueObjects;
using NSubstitute;
namespace CardLedger.Application.Tests;

public class PurchaseServiceTests
{
    private readonly ICardRepository _cardRepository = Substitute.For<ICardRepository>();
    private readonly ILedgerRepository _ledgerRepository = Substitute.For<ILedgerRepository>();
    private readonly ITransactionRepository _transactionRepository = Substitute.For<ITransactionRepository>();
    private readonly IExchangeRateRepository _exchangeRateRepository = Substitute.For<IExchangeRateRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly PurchaseService _sut;

    public PurchaseServiceTests()
    {
        var conversionService = new CurrencyConversionService(_exchangeRateRepository);
        _sut = new PurchaseService(
            _cardRepository,
            _ledgerRepository,
            _transactionRepository,
            conversionService,
            _unitOfWork,
            TestCurrencySupport.CreateValidator("USD", "EUR", "GBP", "AUD", "CAD", "JPY"));
    }
    [Fact]
    public async Task PurchaseAsync_DebitsLedgerForValidPurchase()
    {
        var cardId = Guid.NewGuid();
        const string pan = "4111111111111111";
        const string cvv = "123";
        var cardExpiry = CardExpiry.FromIssueDate(DateOnly.FromDateTime(DateTime.UtcNow));
        var card = new Card
        {
            Id = cardId,
            Pan = pan,
            ExpiryDate = cardExpiry.EndOfMonthDate,
            CvvHash = CvvHasher.Hash(cvv),
            CreditLimit = 1000m,
            Currency = "USD",
            IssuedAt = DateTimeOffset.UtcNow
        };

        var ledger = new Ledger
        {
            Id = Guid.NewGuid(),
            CardId = cardId,
            AvailableBalance = 1000m,
            Currency = "USD",
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _cardRepository.GetByPanAsync(pan, Arg.Any<CancellationToken>()).Returns(card);
        _ledgerRepository.GetByCardIdAsync(cardId, Arg.Any<CancellationToken>()).Returns(ledger);

        var response = await _sut.PurchaseAsync(new PurchaseRequest(
            pan,
            cardExpiry.MmYy,
            cvv,
            100m,
            "USD",
            "Test purchase"));

        Assert.Equal(100m, response.Amount);
        Assert.Equal(900m, ledger.AvailableBalance);
        await _transactionRepository.Received(1).AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PurchaseAsync_ThrowsWhenBalanceInsufficient()
    {
        var cardId = Guid.NewGuid();
        const string pan = "4222222222222222";
        const string cvv = "456";
        var cardExpiry = CardExpiry.FromIssueDate(DateOnly.FromDateTime(DateTime.UtcNow));
        var card = new Card
        {
            Id = cardId,
            Pan = pan,
            ExpiryDate = cardExpiry.EndOfMonthDate,
            CvvHash = CvvHasher.Hash(cvv),
            CreditLimit = 100m,
            Currency = "USD",
            IssuedAt = DateTimeOffset.UtcNow
        };

        var ledger = new Ledger
        {
            Id = Guid.NewGuid(),
            CardId = cardId,
            AvailableBalance = 50m,
            Currency = "USD",
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _cardRepository.GetByPanAsync(pan, Arg.Any<CancellationToken>()).Returns(card);
        _ledgerRepository.GetByCardIdAsync(cardId, Arg.Any<CancellationToken>()).Returns(ledger);

        await Assert.ThrowsAsync<InsufficientBalanceException>(() =>
            _sut.PurchaseAsync(new PurchaseRequest(
                pan,
                cardExpiry.MmYy,
                cvv,
                100m,
                "USD",
                "Too much")));
    }
}

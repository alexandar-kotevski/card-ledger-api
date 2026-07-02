using CardLedger.Application.Abstractions;
using CardLedger.Application.DTOs;
using CardLedger.Application.Services;
using CardLedger.Domain.Entities;
using NSubstitute;

namespace CardLedger.Application.Tests;

public class IssueCardServiceTests
{
    private readonly ICardRepository _cardRepository = Substitute.For<ICardRepository>();
    private readonly ILedgerRepository _ledgerRepository = Substitute.For<ILedgerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IssueCardService _sut;

    public IssueCardServiceTests()
    {
        _sut = new IssueCardService(_cardRepository, _ledgerRepository, _unitOfWork);
    }

    [Fact]
    public async Task IssueAsync_CreatesCardAndLedger()
    {
        _cardRepository.ExistsByPanAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        Card? savedCard = null;
        Ledger? savedLedger = null;

        await _cardRepository.AddAsync(Arg.Do<Card>(c => savedCard = c), Arg.Any<CancellationToken>());
        await _ledgerRepository.AddAsync(Arg.Do<Ledger>(l => savedLedger = l), Arg.Any<CancellationToken>());

        var response = await _sut.IssueAsync(new IssueCardRequest(5000m, "USD"));

        Assert.Equal(16, response.CardNumber.Length);
        Assert.Equal("USD", response.Currency);
        Assert.Equal(5000m, response.CreditLimit);
        Assert.Equal(3, response.Cvv.Length);
        Assert.NotNull(savedCard);
        Assert.NotNull(savedLedger);
        Assert.Equal(savedCard!.Id, savedLedger!.CardId);
        Assert.Equal(5000m, savedLedger.AvailableBalance);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IssueAsync_RejectsNonPositiveCreditLimit()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _sut.IssueAsync(new IssueCardRequest(0m, "USD")));
    }
}

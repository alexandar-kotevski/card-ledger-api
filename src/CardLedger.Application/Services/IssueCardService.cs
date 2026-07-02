using System.Security.Cryptography;
using CardLedger.Application.Abstractions;
using CardLedger.Application.DTOs;
using CardLedger.Domain.Entities;
using CardLedger.Domain.Services;
using CardLedger.Domain.ValueObjects;

namespace CardLedger.Application.Services;

public sealed class IssueCardService
{
    private readonly ICardRepository _cardRepository;
    private readonly ILedgerRepository _ledgerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public IssueCardService(
        ICardRepository cardRepository,
        ILedgerRepository ledgerRepository,
        IUnitOfWork unitOfWork)
    {
        _cardRepository = cardRepository;
        _ledgerRepository = ledgerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IssueCardResponse> IssueAsync(
        IssueCardRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.CreditLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.CreditLimit), "Credit limit must be positive.");
        }

        var currency = CurrencyCode.Create(request.Currency);
        var issuedAt = DateTimeOffset.UtcNow;
        var expiryDate = DateOnly.FromDateTime(issuedAt.UtcDateTime).AddYears(3);
        var cvv = GenerateCvv();
        var pan = await GenerateUniquePanAsync(cancellationToken).ConfigureAwait(false);

        var card = new Card
        {
            Id = Guid.NewGuid(),
            Pan = pan,
            ExpiryDate = expiryDate,
            CvvHash = CvvHasher.Hash(cvv),
            CreditLimit = request.CreditLimit,
            Currency = currency.Value,
            IssuedAt = issuedAt
        };

        var ledger = new Ledger
        {
            Id = Guid.NewGuid(),
            CardId = card.Id,
            AvailableBalance = request.CreditLimit,
            Currency = currency.Value,
            UpdatedAt = issuedAt
        };

        await _cardRepository.AddAsync(card, cancellationToken).ConfigureAwait(false);
        await _ledgerRepository.AddAsync(ledger, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new IssueCardResponse(pan, expiryDate, cvv, currency.Value, request.CreditLimit);
    }

    private async Task<string> GenerateUniquePanAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var pan = CardNumberGenerator.Generate();
            if (!await _cardRepository.ExistsByPanAsync(pan, cancellationToken).ConfigureAwait(false))
            {
                return pan;
            }
        }

        throw new InvalidOperationException("Unable to generate a unique card number.");
    }

    private static string GenerateCvv()
    {
        Span<byte> bytes = stackalloc byte[2];
        RandomNumberGenerator.Fill(bytes);
        return (BitConverter.ToUInt16(bytes) % 1000).ToString("D3");
    }
}

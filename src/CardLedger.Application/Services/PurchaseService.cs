using CardLedger.Application.Abstractions;
using CardLedger.Application.DTOs;
using CardLedger.Domain.Entities;
using CardLedger.Domain.Exceptions;
using CardLedger.Domain.Services;
using CardLedger.Domain.ValueObjects;

namespace CardLedger.Application.Services;

public sealed class PurchaseService
{
    private readonly ICardRepository _cardRepository;
    private readonly ILedgerRepository _ledgerRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly CurrencyConversionService _currencyConversionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CurrencyValidator _currencyValidator;

    public PurchaseService(
        ICardRepository cardRepository,
        ILedgerRepository ledgerRepository,
        ITransactionRepository transactionRepository,
        CurrencyConversionService currencyConversionService,
        IUnitOfWork unitOfWork,
        CurrencyValidator currencyValidator)
    {
        _cardRepository = cardRepository;
        _ledgerRepository = ledgerRepository;
        _transactionRepository = transactionRepository;
        _currencyConversionService = currencyConversionService;
        _unitOfWork = unitOfWork;
        _currencyValidator = currencyValidator;
    }

    public async Task<PurchaseResponse> PurchaseAsync(
        PurchaseRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = Pan.Create(request.CardNumber);
        _ = Cvv.Create(request.Cvv);
        var purchaseCurrency = _currencyValidator.ValidateSupported(request.Currency);

        var cardExpiry = CardExpiry.Parse(request.ExpiryDate);

        var card = await _cardRepository
            .GetByPanAsync(request.CardNumber, cancellationToken)
            .ConfigureAwait(false);

        if (card is null)
        {
            throw new CardNotFoundException(request.CardNumber);
        }

        if (card.ExpiryDate != cardExpiry.EndOfMonthDate || !CvvHasher.Verify(request.Cvv, card.CvvHash))
        {
            throw new InvalidCardCredentialsException();
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (card.ExpiryDate < today)
        {
            throw new CardExpiredException(request.CardNumber, card.ExpiryDate);
        }

        var ledger = await _ledgerRepository
            .GetByCardIdAsync(card.Id, cancellationToken)
            .ConfigureAwait(false);

        if (ledger is null)
        {
            throw new InvalidOperationException($"Ledger not found for card {request.CardNumber}.");
        }

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            CardId = card.Id,
            Description = request.Description,
            TransactionDate = DateTimeOffset.UtcNow,
            Amount = request.Amount,
            Currency = purchaseCurrency.Value
        };

        if (request.Amount > 0)
        {
            var debitAmount = request.Amount;
            if (purchaseCurrency.Value != card.Currency)
            {
                var conversion = await _currencyConversionService
                    .ConvertUsingLatestRateAsync(
                        request.Amount,
                        purchaseCurrency.Value,
                        card.Currency,
                        request.CardNumber,
                        cancellationToken)
                    .ConfigureAwait(false);

                debitAmount = conversion.ConvertedAmount;
            }

            if (debitAmount > ledger.AvailableBalance)
            {
                throw new InsufficientBalanceException(
                    request.CardNumber,
                    debitAmount,
                    ledger.AvailableBalance);
            }

            ledger.AvailableBalance -= debitAmount;
            ledger.UpdatedAt = DateTimeOffset.UtcNow;
            _ledgerRepository.Update(ledger);
        }

        await _transactionRepository.AddAsync(transaction, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new PurchaseResponse(
            transaction.Id,
            transaction.Amount,
            transaction.Currency,
            transaction.Description);
    }
}

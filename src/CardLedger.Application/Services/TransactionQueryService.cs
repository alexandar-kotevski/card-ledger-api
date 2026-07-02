using CardLedger.Application.Abstractions;
using CardLedger.Application.DTOs;
using CardLedger.Domain.Exceptions;

namespace CardLedger.Application.Services;

public sealed class TransactionQueryService
{
    private readonly ICardRepository _cardRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ExchangeRateLookbackService _exchangeRateLookbackService;

    public TransactionQueryService(
        ICardRepository cardRepository,
        ITransactionRepository transactionRepository,
        ExchangeRateLookbackService exchangeRateLookbackService)
    {
        _cardRepository = cardRepository;
        _transactionRepository = transactionRepository;
        _exchangeRateLookbackService = exchangeRateLookbackService;
    }

    public async Task<IReadOnlyList<TransactionDetailDto>> ListAsync(
        string cardNumber,
        string? targetCurrency,
        CancellationToken cancellationToken = default)
    {
        await EnsureCardExistsAsync(cardNumber, cancellationToken).ConfigureAwait(false);

        var transactions = await _transactionRepository
            .GetByCardPanAsync(cardNumber, cancellationToken)
            .ConfigureAwait(false);

        var results = new List<TransactionDetailDto>(transactions.Count);
        foreach (var transaction in transactions)
        {
            results.Add(await MapTransactionAsync(transaction, cardNumber, targetCurrency, cancellationToken)
                .ConfigureAwait(false));
        }

        return results;
    }

    public async Task<TransactionDetailDto> GetByIdAsync(
        string cardNumber,
        Guid transactionId,
        string? targetCurrency,
        CancellationToken cancellationToken = default)
    {
        await EnsureCardExistsAsync(cardNumber, cancellationToken).ConfigureAwait(false);

        var transaction = await _transactionRepository
            .GetByCardPanAndIdAsync(cardNumber, transactionId, cancellationToken)
            .ConfigureAwait(false);

        if (transaction is null)
        {
            throw new KeyNotFoundException($"Transaction {transactionId} was not found for card {cardNumber}.");
        }

        return await MapTransactionAsync(transaction, cardNumber, targetCurrency, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<TransactionDetailDto> MapTransactionAsync(
        Domain.Entities.Transaction transaction,
        string cardNumber,
        string? targetCurrency,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(targetCurrency))
        {
            return new TransactionDetailDto(
                transaction.Id,
                transaction.Description,
                transaction.TransactionDate,
                transaction.Amount,
                transaction.Currency);
        }

        var conversion = await _exchangeRateLookbackService
            .ConvertForTransactionAsync(
                transaction.Amount,
                transaction.Currency,
                targetCurrency,
                transaction.TransactionDate,
                cardNumber,
                transaction.Id,
                cancellationToken)
            .ConfigureAwait(false);

        return new TransactionDetailDto(
            transaction.Id,
            transaction.Description,
            transaction.TransactionDate,
            transaction.Amount,
            transaction.Currency,
            conversion.ConvertedAmount,
            conversion.TargetCurrency,
            conversion.TargetRate ?? conversion.SourceRate,
            conversion.RateDate);
    }

    private async Task EnsureCardExistsAsync(string cardNumber, CancellationToken cancellationToken)
    {
        if (await _cardRepository.GetByPanAsync(cardNumber, cancellationToken).ConfigureAwait(false) is null)
        {
            throw new CardNotFoundException(cardNumber);
        }
    }
}

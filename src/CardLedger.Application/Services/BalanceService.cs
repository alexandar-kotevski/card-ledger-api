using CardLedger.Application.Abstractions;
using CardLedger.Application.DTOs;
using CardLedger.Domain.Exceptions;
using CardLedger.Domain.ValueObjects;

namespace CardLedger.Application.Services;

public sealed class BalanceService
{
    private readonly ICardRepository _cardRepository;
    private readonly ILedgerRepository _ledgerRepository;
    private readonly ExchangeRateLookbackService _exchangeRateLookbackService;

    public BalanceService(
        ICardRepository cardRepository,
        ILedgerRepository ledgerRepository,
        ExchangeRateLookbackService exchangeRateLookbackService)
    {
        _cardRepository = cardRepository;
        _ledgerRepository = ledgerRepository;
        _exchangeRateLookbackService = exchangeRateLookbackService;
    }

    public async Task<BalanceResponse> GetBalanceAsync(
        string cardNumber,
        string? targetCurrency,
        CancellationToken cancellationToken = default)
    {
        var card = await _cardRepository
            .GetByPanAsync(cardNumber, cancellationToken)
            .ConfigureAwait(false);

        if (card is null)
        {
            throw new CardNotFoundException(cardNumber);
        }

        var ledger = await _ledgerRepository
            .GetByCardIdAsync(card.Id, cancellationToken)
            .ConfigureAwait(false);

        if (ledger is null)
        {
            throw new InvalidOperationException($"Ledger not found for card {cardNumber}.");
        }

        if (string.IsNullOrWhiteSpace(targetCurrency) ||
            targetCurrency.Equals(ledger.Currency, StringComparison.OrdinalIgnoreCase))
        {
            return new BalanceResponse(
                ledger.AvailableBalance,
                ledger.Currency);
        }

        var conversion = await _exchangeRateLookbackService
            .ConvertBalanceUsingLatestRateAsync(
                ledger.AvailableBalance,
                ledger.Currency,
                targetCurrency,
                cardNumber,
                cancellationToken)
            .ConfigureAwait(false);

        return new BalanceResponse(
            ledger.AvailableBalance,
            ledger.Currency,
            conversion.ConvertedAmount,
            conversion.TargetCurrency,
            conversion.AppliedRate,
            conversion.RateDate);
    }
}

using CardLedger.Application.Abstractions;
using CardLedger.Domain.Exceptions;

namespace CardLedger.Application.Services;

public sealed class ExchangeRateLookbackService
{
    private readonly IExchangeRateRepository _exchangeRateRepository;

    public ExchangeRateLookbackService(IExchangeRateRepository exchangeRateRepository)
    {
        _exchangeRateRepository = exchangeRateRepository;
    }

    public async Task<ConversionResult> ConvertForTransactionAsync(
        decimal amount,
        string sourceCurrency,
        string? targetCurrency,
        DateTimeOffset transactionDate,
        string cardNumber,
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetCurrency))
        {
            return new ConversionResult(amount, sourceCurrency.ToUpperInvariant(), null, null, null);
        }

        var normalizedTarget = targetCurrency.ToUpperInvariant();
        var transactionDateOnly = DateOnly.FromDateTime(transactionDate.UtcDateTime);
        var windowStart = transactionDateOnly.AddMonths(-6);

        var normalizedSource = sourceCurrency.ToUpperInvariant();
        if (normalizedSource == normalizedTarget)
        {
            return new ConversionResult(amount, normalizedTarget, null, null, null);
        }

        var sourceRate = await GetRateInWindowOrThrowAsync(
            normalizedSource,
            transactionDateOnly,
            windowStart,
            normalizedSource,
            normalizedTarget,
            cardNumber,
            transactionId,
            transactionDate,
            cancellationToken).ConfigureAwait(false);

        var targetRate = await GetRateInWindowOrThrowAsync(
            normalizedTarget,
            transactionDateOnly,
            windowStart,
            normalizedSource,
            normalizedTarget,
            cardNumber,
            transactionId,
            transactionDate,
            cancellationToken).ConfigureAwait(false);

        var converted = CurrencyConversionService.ConvertViaUsd(
            amount,
            normalizedSource,
            normalizedTarget,
            sourceRate.Rate,
            targetRate.Rate);

        var rateDate = ConversionRateMetadata.ResolveRateDate(
            normalizedSource,
            normalizedTarget,
            normalizedSource == "USD" ? null : sourceRate.RecordDate,
            normalizedTarget == "USD" ? null : targetRate.RecordDate);

        return new ConversionResult(
            converted,
            normalizedTarget,
            normalizedSource == "USD" ? null : sourceRate.Rate,
            normalizedTarget == "USD" ? null : targetRate.Rate,
            rateDate);
    }

    public async Task<ConversionResult> ConvertBalanceUsingLatestRateAsync(
        decimal amount,
        string sourceCurrency,
        string targetCurrency,
        string cardNumber,
        CancellationToken cancellationToken = default)
    {
        var normalizedSource = sourceCurrency.ToUpperInvariant();
        var normalizedTarget = targetCurrency.ToUpperInvariant();

        if (normalizedSource == normalizedTarget)
        {
            return new ConversionResult(amount, normalizedTarget, null, null, null);
        }

        var sourceRate = await GetLatestRateOrThrowAsync(
            normalizedSource,
            normalizedSource,
            normalizedTarget,
            cardNumber,
            cancellationToken).ConfigureAwait(false);

        var targetRate = await GetLatestRateOrThrowAsync(
            normalizedTarget,
            normalizedSource,
            normalizedTarget,
            cardNumber,
            cancellationToken).ConfigureAwait(false);

        var converted = CurrencyConversionService.ConvertViaUsd(
            amount,
            normalizedSource,
            normalizedTarget,
            sourceRate.Rate,
            targetRate.Rate);

        var rateDate = ConversionRateMetadata.ResolveRateDate(
            normalizedSource,
            normalizedTarget,
            normalizedSource == "USD" ? null : sourceRate.RecordDate,
            normalizedTarget == "USD" ? null : targetRate.RecordDate);

        return new ConversionResult(
            converted,
            normalizedTarget,
            normalizedSource == "USD" ? null : sourceRate.Rate,
            normalizedTarget == "USD" ? null : targetRate.Rate,
            rateDate);
    }

    private async Task<Domain.Entities.ExchangeRate> GetRateInWindowOrThrowAsync(
        string currencyCode,
        DateOnly transactionDate,
        DateOnly windowStart,
        string sourceCurrency,
        string targetCurrency,
        string cardNumber,
        Guid transactionId,
        DateTimeOffset transactionDateTime,
        CancellationToken cancellationToken)
    {
        if (currencyCode == "USD")
        {
            return new Domain.Entities.ExchangeRate
            {
                CurrencyCode = "USD",
                CountryCurrencyDesc = "United States-Dollar",
                Rate = 1m,
                RecordDate = transactionDate
            };
        }

        var rate = await _exchangeRateRepository
            .GetMostRecentInWindowAsync(currencyCode, transactionDate, windowStart, cancellationToken)
            .ConfigureAwait(false);

        if (rate is null)
        {
            throw new ExchangeRateNotFoundException(
                sourceCurrency,
                targetCurrency,
                cardNumber,
                transactionId,
                transactionDateTime);
        }

        return rate;
    }

    private async Task<Domain.Entities.ExchangeRate> GetLatestRateOrThrowAsync(
        string currencyCode,
        string sourceCurrency,
        string targetCurrency,
        string cardNumber,
        CancellationToken cancellationToken)
    {
        if (currencyCode == "USD")
        {
            return new Domain.Entities.ExchangeRate
            {
                CurrencyCode = "USD",
                CountryCurrencyDesc = "United States-Dollar",
                Rate = 1m,
                RecordDate = DateOnly.FromDateTime(DateTime.UtcNow)
            };
        }

        var rate = await _exchangeRateRepository
            .GetLatestRateAsync(currencyCode, cancellationToken)
            .ConfigureAwait(false);

        if (rate is null)
        {
            throw new ExchangeRateNotFoundException(
                sourceCurrency,
                targetCurrency,
                cardNumber);
        }

        return rate;
    }
}

using CardLedger.Application.Abstractions;
using CardLedger.Domain.Exceptions;

namespace CardLedger.Application.Services;

public sealed class CurrencyConversionService
{
    private readonly IExchangeRateRepository _exchangeRateRepository;

    public CurrencyConversionService(IExchangeRateRepository exchangeRateRepository)
    {
        _exchangeRateRepository = exchangeRateRepository;
    }

    public async Task<ConversionResult> ConvertUsingLatestRateAsync(
        decimal amount,
        string sourceCurrency,
        string targetCurrency,
        string? cardNumber = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedSource = sourceCurrency.ToUpperInvariant();
        var normalizedTarget = targetCurrency.ToUpperInvariant();

        if (normalizedSource == normalizedTarget)
        {
            return new ConversionResult(amount, normalizedTarget, null, null, null);
        }

        var sourceRate = await GetLatestRateOrThrowAsync(
            normalizedSource, normalizedSource, normalizedTarget, cardNumber, null, null, cancellationToken)
            .ConfigureAwait(false);

        var targetRate = await GetLatestRateOrThrowAsync(
            normalizedTarget, normalizedSource, normalizedTarget, cardNumber, null, null, cancellationToken)
            .ConfigureAwait(false);

        var converted = ConvertViaUsd(amount, normalizedSource, normalizedTarget, sourceRate.Rate, targetRate.Rate);
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

    public async Task<ConversionResult> ConvertUsingHistoricalRatesAsync(
        decimal amount,
        string sourceCurrency,
        string targetCurrency,
        DateOnly transactionDate,
        string? cardNumber = null,
        Guid? transactionId = null,
        DateTimeOffset? transactionDateTime = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedSource = sourceCurrency.ToUpperInvariant();
        var normalizedTarget = targetCurrency.ToUpperInvariant();

        if (normalizedSource == normalizedTarget)
        {
            return new ConversionResult(amount, normalizedTarget, null, null, null);
        }

        var windowStart = transactionDate.AddMonths(-6);

        var sourceRate = await GetHistoricalRateOrThrowAsync(
            normalizedSource,
            transactionDate,
            windowStart,
            normalizedSource,
            normalizedTarget,
            cardNumber,
            transactionId,
            transactionDateTime,
            cancellationToken).ConfigureAwait(false);

        var targetRate = await GetHistoricalRateOrThrowAsync(
            normalizedTarget,
            transactionDate,
            windowStart,
            normalizedSource,
            normalizedTarget,
            cardNumber,
            transactionId,
            transactionDateTime,
            cancellationToken).ConfigureAwait(false);

        var converted = ConvertViaUsd(amount, normalizedSource, normalizedTarget, sourceRate.Rate, targetRate.Rate);
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

    public static decimal ConvertViaUsd(
        decimal amount,
        string sourceCurrency,
        string targetCurrency,
        decimal sourceRate,
        decimal targetRate)
    {
        var usdAmount = sourceCurrency == "USD" ? amount : amount / sourceRate;
        var targetAmount = targetCurrency == "USD" ? usdAmount : usdAmount * targetRate;
        return Math.Round(targetAmount, 4, MidpointRounding.ToEven);
    }

    private async Task<Domain.Entities.ExchangeRate> GetLatestRateOrThrowAsync(
        string currencyCode,
        string sourceCurrency,
        string targetCurrency,
        string? cardNumber,
        Guid? transactionId,
        DateTimeOffset? transactionDate,
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
                cardNumber,
                transactionId,
                transactionDate);
        }

        return rate;
    }

    private async Task<Domain.Entities.ExchangeRate> GetHistoricalRateOrThrowAsync(
        string currencyCode,
        DateOnly transactionDate,
        DateOnly windowStart,
        string sourceCurrency,
        string targetCurrency,
        string? cardNumber,
        Guid? transactionId,
        DateTimeOffset? transactionDateTime,
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
}

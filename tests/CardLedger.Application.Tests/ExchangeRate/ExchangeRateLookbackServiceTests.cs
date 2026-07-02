using CardLedger.Application.Abstractions;
using CardLedger.Application.Services;
using CardLedger.Domain.Entities;
using CardLedger.Domain.Exceptions;
using NSubstitute;
using ExchangeRateEntity = CardLedger.Domain.Entities.ExchangeRate;

namespace CardLedger.Application.Tests.ExchangeRateLookback;

public class ExchangeRateLookbackServiceTests
{
    private readonly IExchangeRateRepository _repository = Substitute.For<IExchangeRateRepository>();
    private readonly ExchangeRateLookbackService _sut;

    public ExchangeRateLookbackServiceTests()
    {
        _sut = new ExchangeRateLookbackService(_repository);
    }

    [Fact]
    public async Task ConvertForTransactionAsync_ReturnsOriginalAmountWhenTargetCurrencyOmitted()
    {
        var transactionDate = DateTimeOffset.UtcNow;

        var result = await _sut.ConvertForTransactionAsync(
            150m,
            "USD",
            null,
            transactionDate,
            "4111111111111111",
            Guid.NewGuid());

        Assert.Equal(150m, result.ConvertedAmount);
        Assert.Equal("USD", result.TargetCurrency);
        await _repository.DidNotReceiveWithAnyArgs().GetMostRecentInWindowAsync(default!, default, default, default);
    }

    [Fact]
    public async Task ConvertForTransactionAsync_ReturnsOriginalAmountWhenTargetCurrencyWhitespace()
    {
        var result = await _sut.ConvertForTransactionAsync(
            75m,
            "EUR",
            "   ",
            DateTimeOffset.UtcNow,
            "4111111111111111",
            Guid.NewGuid());

        Assert.Equal(75m, result.ConvertedAmount);
        Assert.Equal("EUR", result.TargetCurrency);
    }

    [Fact]
    public async Task ConvertForTransactionAsync_UsesMostRecentRateWithinWindow()
    {
        var transactionDate = new DateTimeOffset(2026, 7, 2, 12, 0, 0, TimeSpan.Zero);
        var transactionDateOnly = DateOnly.FromDateTime(transactionDate.UtcDateTime);
        var windowStart = transactionDateOnly.AddMonths(-6);

        _repository.GetMostRecentInWindowAsync("EUR", transactionDateOnly, windowStart, Arg.Any<CancellationToken>())
            .Returns(new ExchangeRateEntity
            {
                CurrencyCode = "EUR",
                CountryCurrencyDesc = "Euro-Euro",
                Rate = 0.90m,
                RecordDate = transactionDateOnly.AddDays(-10)
            });

        _repository.GetMostRecentInWindowAsync("GBP", transactionDateOnly, windowStart, Arg.Any<CancellationToken>())
            .Returns(new ExchangeRateEntity
            {
                CurrencyCode = "GBP",
                CountryCurrencyDesc = "United Kingdom-Pound",
                Rate = 0.80m,
                RecordDate = transactionDateOnly.AddDays(-5)
            });

        var result = await _sut.ConvertForTransactionAsync(
            100m,
            "EUR",
            "GBP",
            transactionDate,
            "4111111111111111",
            Guid.NewGuid());

        Assert.Equal("GBP", result.TargetCurrency);
        Assert.Equal(88.8889m, result.ConvertedAmount);
        Assert.Equal(0.80m, result.TargetRate);
        Assert.Equal(transactionDateOnly.AddDays(-5), result.RateDate);
    }

    [Fact]
    public async Task ConvertForTransactionAsync_IncludesRateOnSixMonthBoundary()
    {
        var transactionDate = new DateTimeOffset(2026, 7, 2, 0, 0, 0, TimeSpan.Zero);
        var transactionDateOnly = DateOnly.FromDateTime(transactionDate.UtcDateTime);
        var boundaryDate = transactionDateOnly.AddMonths(-6);

        _repository.GetMostRecentInWindowAsync("EUR", transactionDateOnly, boundaryDate, Arg.Any<CancellationToken>())
            .Returns(new ExchangeRateEntity
            {
                CurrencyCode = "EUR",
                CountryCurrencyDesc = "Euro-Euro",
                Rate = 0.90m,
                RecordDate = boundaryDate
            });

        var result = await _sut.ConvertForTransactionAsync(
            90m,
            "EUR",
            "USD",
            transactionDate,
            "4111111111111111",
            Guid.NewGuid());

        Assert.Equal(100m, result.ConvertedAmount);
        Assert.Equal(boundaryDate, result.RateDate);
    }

    [Fact]
    public async Task ConvertForTransactionAsync_ThrowsWhenRateMissingInWindow()
    {
        var transactionDate = new DateTimeOffset(2026, 7, 2, 0, 0, 0, TimeSpan.Zero);
        var transactionDateOnly = DateOnly.FromDateTime(transactionDate.UtcDateTime);
        var windowStart = transactionDateOnly.AddMonths(-6);
        var transactionId = Guid.NewGuid();

        _repository.GetMostRecentInWindowAsync("EUR", transactionDateOnly, windowStart, Arg.Any<CancellationToken>())
            .Returns((ExchangeRateEntity?)null);

        var ex = await Assert.ThrowsAsync<ExchangeRateNotFoundException>(() =>
            _sut.ConvertForTransactionAsync(
                100m,
                "EUR",
                "USD",
                transactionDate,
                "4111111111111111",
                transactionId));

        Assert.Equal("4111111111111111", ex.CardNumber);
        Assert.Equal(transactionId, ex.TransactionId);
        Assert.Equal("EUR", ex.SourceCurrency);
        Assert.Equal("USD", ex.TargetCurrency);
        Assert.Equal(transactionDate, ex.TransactionDate);
    }

    [Fact]
    public async Task ConvertForTransactionAsync_ReturnsSameAmountForSameCurrency()
    {
        var transactionDate = DateTimeOffset.UtcNow;

        var result = await _sut.ConvertForTransactionAsync(
            250m,
            "USD",
            "USD",
            transactionDate,
            "4111111111111111",
            Guid.NewGuid());

        Assert.Equal(250m, result.ConvertedAmount);
        Assert.Equal("USD", result.TargetCurrency);
        Assert.Null(result.SourceRate);
        Assert.Null(result.TargetRate);
        await _repository.DidNotReceiveWithAnyArgs().GetMostRecentInWindowAsync(default!, default, default, default);
    }

    [Fact]
    public async Task ConvertBalanceUsingLatestRateAsync_ConvertsCrossCurrencyUsingLatestRates()
    {
        _repository.GetLatestRateAsync("EUR", Arg.Any<CancellationToken>())
            .Returns(new ExchangeRateEntity
            {
                CurrencyCode = "EUR",
                CountryCurrencyDesc = "Euro-Euro",
                Rate = 0.90m,
                RecordDate = new DateOnly(2026, 6, 30)
            });

        _repository.GetLatestRateAsync("GBP", Arg.Any<CancellationToken>())
            .Returns(new ExchangeRateEntity
            {
                CurrencyCode = "GBP",
                CountryCurrencyDesc = "United Kingdom-Pound",
                Rate = 0.80m,
                RecordDate = new DateOnly(2026, 6, 29)
            });

        var result = await _sut.ConvertBalanceUsingLatestRateAsync(
            90m,
            "EUR",
            "GBP",
            "4111111111111111");

        Assert.Equal(80m, result.ConvertedAmount);
        Assert.Equal(new DateOnly(2026, 6, 30), result.RateDate);
    }

    [Fact]
    public async Task ConvertBalanceUsingLatestRateAsync_ReturnsSameAmountForSameCurrency()
    {
        var result = await _sut.ConvertBalanceUsingLatestRateAsync(500m, "USD", "USD", "4111111111111111");
        Assert.Equal(500m, result.ConvertedAmount);
        await _repository.DidNotReceiveWithAnyArgs().GetLatestRateAsync(default!, default);
    }

    [Fact]
    public async Task ConvertBalanceUsingLatestRateAsync_UsesLatestRate()
    {
        _repository.GetLatestRateAsync("EUR", Arg.Any<CancellationToken>())
            .Returns(new ExchangeRateEntity
            {
                CurrencyCode = "EUR",
                CountryCurrencyDesc = "Euro-Euro",
                Rate = 0.90m,
                RecordDate = new DateOnly(2026, 6, 30)
            });

        var result = await _sut.ConvertBalanceUsingLatestRateAsync(
            90m,
            "EUR",
            "USD",
            "4111111111111111");

        Assert.Equal(100m, result.ConvertedAmount);
        Assert.Equal(new DateOnly(2026, 6, 30), result.RateDate);
    }

    [Fact]
    public async Task ConvertBalanceUsingLatestRateAsync_ThrowsWhenLatestRateMissing()
    {
        _repository.GetLatestRateAsync("EUR", Arg.Any<CancellationToken>())
            .Returns((ExchangeRateEntity?)null);

        await Assert.ThrowsAsync<ExchangeRateNotFoundException>(() =>
            _sut.ConvertBalanceUsingLatestRateAsync(
                100m,
                "EUR",
                "USD",
                "4111111111111111"));
    }
}

using CardLedger.Application.Abstractions;
using CardLedger.Application.Services;
using CardLedger.Domain.Entities;
using CardLedger.Domain.Exceptions;
using NSubstitute;
using ExchangeRateEntity = CardLedger.Domain.Entities.ExchangeRate;

namespace CardLedger.Application.Tests.ExchangeRate;

public class CurrencyConversionServiceTests
{
    private readonly IExchangeRateRepository _repository = Substitute.For<IExchangeRateRepository>();
    private readonly CurrencyConversionService _sut;

    public CurrencyConversionServiceTests()
    {
        _sut = new CurrencyConversionService(_repository);
    }

    [Fact]
    public async Task ConvertUsingLatestRateAsync_ReturnsSameAmountForSameCurrency()
    {
        var result = await _sut.ConvertUsingLatestRateAsync(100m, "USD", "USD");
        Assert.Equal(100m, result.ConvertedAmount);
        await _repository.DidNotReceiveWithAnyArgs().GetLatestRateAsync(default!, default);
    }

    [Fact]
    public async Task ConvertUsingLatestRateAsync_ConvertsUsingLatestRates()
    {
        _repository.GetLatestRateAsync("EUR", Arg.Any<CancellationToken>())
            .Returns(new ExchangeRateEntity
            {
                CurrencyCode = "EUR",
                CountryCurrencyDesc = "Euro-Euro",
                Rate = 0.90m,
                EffectiveDate = new DateOnly(2026, 6, 30),
                RecordDate = new DateOnly(2026, 6, 30)
            });

        var result = await _sut.ConvertUsingLatestRateAsync(90m, "EUR", "USD");
        Assert.Equal(100m, result.ConvertedAmount);
    }

    [Fact]
    public async Task ConvertUsingLatestRateAsync_ThrowsWhenRateMissing()
    {
        _repository.GetLatestRateAsync("EUR", Arg.Any<CancellationToken>())
            .Returns((ExchangeRateEntity?)null);

        await Assert.ThrowsAsync<ExchangeRateNotFoundException>(() =>
            _sut.ConvertUsingLatestRateAsync(100m, "EUR", "USD", "4111111111111111"));
    }

    [Fact]
    public async Task ConvertUsingHistoricalRatesAsync_ReturnsSameAmountForSameCurrency()
    {
        var result = await _sut.ConvertUsingHistoricalRatesAsync(
            200m,
            "USD",
            "USD",
            new DateOnly(2026, 7, 2));

        Assert.Equal(200m, result.ConvertedAmount);
        await _repository.DidNotReceiveWithAnyArgs().GetMostRecentInWindowAsync(default!, default, default, default);
    }

    [Fact]
    public async Task ConvertUsingHistoricalRatesAsync_ConvertsUsingWindowRates()
    {
        var transactionDate = new DateOnly(2026, 7, 2);
        var windowStart = transactionDate.AddMonths(-6);

        _repository.GetMostRecentInWindowAsync("EUR", transactionDate, windowStart, Arg.Any<CancellationToken>())
            .Returns(new ExchangeRateEntity
            {
                CurrencyCode = "EUR",
                CountryCurrencyDesc = "Euro-Euro",
                Rate = 0.90m,
                EffectiveDate = transactionDate.AddDays(-1),
                RecordDate = transactionDate.AddDays(-1)
            });

        var result = await _sut.ConvertUsingHistoricalRatesAsync(
            90m,
            "EUR",
            "USD",
            transactionDate,
            "4111111111111111",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        Assert.Equal(100m, result.ConvertedAmount);
    }

    [Fact]
    public async Task ConvertUsingHistoricalRatesAsync_ThrowsWhenHistoricalRateMissing()
    {
        var transactionDate = new DateOnly(2026, 7, 2);
        var windowStart = transactionDate.AddMonths(-6);

        _repository.GetMostRecentInWindowAsync("EUR", transactionDate, windowStart, Arg.Any<CancellationToken>())
            .Returns((ExchangeRateEntity?)null);

        await Assert.ThrowsAsync<ExchangeRateNotFoundException>(() =>
            _sut.ConvertUsingHistoricalRatesAsync(
                100m,
                "EUR",
                "USD",
                transactionDate,
                "4111111111111111",
                Guid.NewGuid(),
                DateTimeOffset.UtcNow));
    }
}

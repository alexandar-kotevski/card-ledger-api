using CardLedger.Application.Services;

namespace CardLedger.Application.Tests.ExchangeRateConversion;

public class CurrencyConversionTests
{
    [Theory]
    [InlineData(100, "USD", "USD", 1.0, 1.0, 100)]
    [InlineData(100, "USD", "EUR", 1.0, 0.90, 90)]
    [InlineData(90, "EUR", "USD", 0.90, 1.0, 100)]
    [InlineData(100, "EUR", "GBP", 0.90, 0.80, 88.8889)]
    public void ConvertViaUsd_ConvertsUsingTreasuryConvention(
        decimal amount,
        string sourceCurrency,
        string targetCurrency,
        decimal sourceRate,
        decimal targetRate,
        decimal expected)
    {
        var result = CurrencyConversionService.ConvertViaUsd(
            amount,
            sourceCurrency,
            targetCurrency,
            sourceRate,
            targetRate);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ConvertViaUsd_RoundsToFourDecimalPlaces()
    {
        var result = CurrencyConversionService.ConvertViaUsd(
            10m,
            "EUR",
            "GBP",
            0.912345m,
            0.789012m);

        Assert.Equal(8.6482m, result);
    }
}

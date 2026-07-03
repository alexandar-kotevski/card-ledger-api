using CardLedger.Application.Services;

namespace CardLedger.Application.Tests.ExchangeRate;

public class ConversionRateMetadataTests
{
    [Fact]
    public void ResolveAppliedRate_ReturnsConvertedPerSourceUnit()
    {
        var result = ConversionRateMetadata.ResolveAppliedRate(50m, 29.5939m);
        Assert.Equal(0.591878m, result);
    }

    [Fact]
    public void ResolveAppliedRate_ReturnsNullForZeroSourceAmount()
    {
        Assert.Null(ConversionRateMetadata.ResolveAppliedRate(0m, 10m));
    }

    [Fact]
    public void ResolveRateDate_ReturnsNullForSameCurrency()
    {
        var result = ConversionRateMetadata.ResolveRateDate("USD", "USD", null, null);
        Assert.Null(result);
    }

    [Fact]
    public void ResolveRateDate_ReturnsTargetDateWhenSourceIsUsd()
    {
        var targetDate = new DateOnly(2026, 6, 1);
        var result = ConversionRateMetadata.ResolveRateDate("USD", "EUR", null, targetDate);
        Assert.Equal(targetDate, result);
    }

    [Fact]
    public void ResolveRateDate_ReturnsSourceDateWhenTargetIsUsd()
    {
        var sourceDate = new DateOnly(2026, 5, 1);
        var result = ConversionRateMetadata.ResolveRateDate("EUR", "USD", sourceDate, null);
        Assert.Equal(sourceDate, result);
    }

    [Fact]
    public void ResolveRateDate_ReturnsTargetDateWhenBothPresent()
    {
        var sourceDate = new DateOnly(2026, 3, 31);
        var targetDate = new DateOnly(2026, 1, 15);
        var result = ConversionRateMetadata.ResolveRateDate("USD", "BGN", sourceDate, targetDate);
        Assert.Equal(targetDate, result);
    }

    [Fact]
    public void ResolveRateDate_ReturnsTargetDateForCrossCurrencyConversion()
    {
        var sourceDate = new DateOnly(2026, 6, 10);
        var targetDate = new DateOnly(2026, 6, 5);
        var result = ConversionRateMetadata.ResolveRateDate("EUR", "GBP", sourceDate, targetDate);
        Assert.Equal(targetDate, result);
    }
}

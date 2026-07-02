using CardLedger.Application.Services;

namespace CardLedger.Application.Tests.ExchangeRate;

public class ConversionRateMetadataTests
{
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
    public void ResolveRateDate_ReturnsTargetDateWhenSourceDateNull()
    {
        var targetDate = new DateOnly(2026, 4, 1);
        var result = ConversionRateMetadata.ResolveRateDate("EUR", "GBP", null, targetDate);
        Assert.Equal(targetDate, result);
    }

    [Fact]
    public void ResolveRateDate_ReturnsSourceDateWhenTargetDateNull()
    {
        var sourceDate = new DateOnly(2026, 3, 1);
        var result = ConversionRateMetadata.ResolveRateDate("EUR", "GBP", sourceDate, null);
        Assert.Equal(sourceDate, result);
    }

    [Fact]
    public void ResolveRateDate_ReturnsLatestDateWhenBothPresent()
    {
        var sourceDate = new DateOnly(2026, 6, 10);
        var targetDate = new DateOnly(2026, 6, 5);
        var result = ConversionRateMetadata.ResolveRateDate("EUR", "GBP", sourceDate, targetDate);
        Assert.Equal(sourceDate, result);
    }
}

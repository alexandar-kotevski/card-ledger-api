namespace CardLedger.Application.Services;

internal static class ConversionRateMetadata
{
    public static decimal? ResolveAppliedRate(decimal sourceAmount, decimal convertedAmount)
    {
        if (sourceAmount == 0)
        {
            return null;
        }

        return Math.Round(convertedAmount / sourceAmount, 8, MidpointRounding.ToEven);
    }

    public static DateOnly? ResolveRateDate(
        string sourceCurrency,
        string targetCurrency,
        DateOnly? sourceRateDate,
        DateOnly? targetRateDate)
    {
        if (sourceCurrency == targetCurrency)
        {
            return null;
        }

        if (targetCurrency == "USD")
        {
            return sourceRateDate;
        }

        return targetRateDate;
    }
}

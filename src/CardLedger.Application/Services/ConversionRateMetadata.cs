namespace CardLedger.Application.Services;

internal static class ConversionRateMetadata
{
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

        if (sourceCurrency == "USD")
        {
            return targetRateDate;
        }

        if (targetCurrency == "USD")
        {
            return sourceRateDate;
        }

        if (sourceRateDate is null)
        {
            return targetRateDate;
        }

        if (targetRateDate is null)
        {
            return sourceRateDate;
        }

        return sourceRateDate > targetRateDate ? sourceRateDate : targetRateDate;
    }
}

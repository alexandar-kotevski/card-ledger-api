namespace CardLedger.Application.Services;

public sealed record ConversionResult(
    decimal ConvertedAmount,
    string TargetCurrency,
    decimal? SourceRate,
    decimal? TargetRate,
    DateOnly? RateDate);

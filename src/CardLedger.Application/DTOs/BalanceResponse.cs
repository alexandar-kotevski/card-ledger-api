namespace CardLedger.Application.DTOs;

public sealed record BalanceResponse(
    decimal AvailableBalance,
    string Currency,
    decimal? ConvertedBalance = null,
    string? ConvertedCurrency = null,
    decimal? RateUsed = null,
    DateOnly? RateDate = null);

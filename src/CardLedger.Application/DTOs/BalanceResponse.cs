namespace CardLedger.Application.DTOs;

public sealed record BalanceResponse(
    decimal AvailableBalance,
    string Currency,
    decimal? RateUsed = null,
    DateOnly? RateDate = null);

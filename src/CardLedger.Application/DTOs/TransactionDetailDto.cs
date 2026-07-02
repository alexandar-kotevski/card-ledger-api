namespace CardLedger.Application.DTOs;

public sealed record TransactionDetailDto(
    Guid Id,
    string Description,
    DateTimeOffset TransactionDate,
    decimal Amount,
    string Currency,
    decimal? ConvertedAmount = null,
    string? ConvertedCurrency = null,
    decimal? RateUsed = null,
    DateOnly? RateDate = null);

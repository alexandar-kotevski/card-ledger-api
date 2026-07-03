namespace CardLedger.Api.Contracts;

/// <summary>Request body for issuing a new card.</summary>
/// <param name="CreditLimit">Decimal-precision credit limit as a string (e.g. <c>5000.00</c>).</param>
/// <param name="Currency">ISO 4217 currency code (USD, EUR, GBP, AUD, CAD, JPY).</param>
public sealed record IssueCardApiRequest(string CreditLimit, string Currency);

/// <summary>Response returned when a card is issued successfully.</summary>
/// <param name="CardNumber">16-digit primary account number.</param>
/// <param name="ExpiryDate">Card expiry date (three years from issue).</param>
/// <param name="Cvv">Three-digit CVV — returned once at issuance only.</param>
/// <param name="Currency">Ledger currency for the card.</param>
/// <param name="CreditLimit">Initial credit limit matching the request.</param>
public sealed record IssueCardApiResponse(
    string CardNumber,
    DateOnly ExpiryDate,
    string Cvv,
    string Currency,
    string CreditLimit);

/// <summary>Request body for recording a purchase transaction.</summary>
public sealed record PurchaseApiRequest(
    string CardNumber,
    DateOnly ExpiryDate,
    string Cvv,
    string Amount,
    string Currency,
    string Description);

/// <summary>Response returned when a purchase is recorded successfully.</summary>
public sealed record PurchaseApiResponse(
    Guid Id,
    string Amount,
    string Currency,
    string Description);

/// <summary>Transaction detail returned by list and get endpoints.</summary>
public sealed record TransactionDetailApiResponse(
    Guid Id,
    string Description,
    DateTimeOffset TransactionDate,
    string Amount,
    string Currency,
    string? ConvertedAmount,
    string? ConvertedCurrency,
    string? RateUsed,
    DateOnly? RateDate);

/// <summary>Available balance converted to the requested target currency.</summary>
public sealed record BalanceApiResponse(
    string AvailableBalance,
    string Currency,
    string? RateUsed,
    DateOnly? RateDate);

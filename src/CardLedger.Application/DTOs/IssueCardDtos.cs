namespace CardLedger.Application.DTOs;

public sealed record IssueCardRequest(decimal CreditLimit, string Currency);

public sealed record IssueCardResponse(
    string CardNumber,
    string ExpiryDate,
    string Cvv,
    string Currency,
    decimal CreditLimit);

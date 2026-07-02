namespace CardLedger.Application.DTOs;

public sealed record PurchaseRequest(
    string CardNumber,
    DateOnly ExpiryDate,
    string Cvv,
    decimal Amount,
    string Currency,
    string Description);

public sealed record PurchaseResponse(
    Guid Id,
    decimal Amount,
    string Currency,
    string Description);

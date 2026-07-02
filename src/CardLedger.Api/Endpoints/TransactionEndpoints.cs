using System.Globalization;
using CardLedger.Application.DTOs;
using CardLedger.Application.Services;
using CardLedger.Domain.ValueObjects;

namespace CardLedger.Api.Endpoints;

public static class TransactionEndpoints
{
    public static RouteGroupBuilder MapTransactionEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/transactions", PurchaseAsync)
            .WithName("CreatePurchase")
            .Produces<PurchaseApiResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/{cardNumber}/transactions", ListTransactionsAsync)
            .WithName("ListTransactions")
            .Produces<IReadOnlyList<TransactionDetailApiResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/{cardNumber}/transactions/{transactionId:guid}", GetTransactionAsync)
            .WithName("GetTransaction")
            .Produces<TransactionDetailApiResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return group;
    }

    private static async Task<IResult> PurchaseAsync(
        PurchaseApiRequest request,
        PurchaseService purchaseService,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidatePurchaseRequest(request);
        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        if (!CardEndpoints.TryParseDecimal(request.Amount, out var amount))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["amount"] = ["Amount must be a valid decimal string."]
            });
        }

        var response = await purchaseService.PurchaseAsync(
            new PurchaseRequest(
                request.CardNumber,
                request.ExpiryDate,
                request.Cvv,
                amount,
                request.Currency,
                request.Description),
            cancellationToken).ConfigureAwait(false);

        return Results.Created(
            $"/api/cards/{request.CardNumber}/transactions/{response.Id}",
            new PurchaseApiResponse(
                response.Id,
                CardEndpoints.FormatDecimal(response.Amount),
                response.Currency,
                response.Description));
    }

    private static async Task<IResult> ListTransactionsAsync(
        string cardNumber,
        string? targetCurrency,
        TransactionQueryService transactionQueryService,
        CancellationToken cancellationToken)
    {
        if (!IsValidPan(cardNumber))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["cardNumber"] = ["Card number must be exactly 16 numeric digits."]
            });
        }

        if (targetCurrency is not null)
        {
            try
            {
                _ = CurrencyCode.Create(targetCurrency);
            }
            catch (ArgumentException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["targetCurrency"] = [ex.Message]
                });
            }
        }

        var transactions = await transactionQueryService
            .ListAsync(cardNumber, targetCurrency, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(transactions.Select(MapTransaction).ToList());
    }

    private static async Task<IResult> GetTransactionAsync(
        string cardNumber,
        Guid transactionId,
        string? targetCurrency,
        TransactionQueryService transactionQueryService,
        CancellationToken cancellationToken)
    {
        if (!IsValidPan(cardNumber))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["cardNumber"] = ["Card number must be exactly 16 numeric digits."]
            });
        }

        if (targetCurrency is not null)
        {
            try
            {
                _ = CurrencyCode.Create(targetCurrency);
            }
            catch (ArgumentException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["targetCurrency"] = [ex.Message]
                });
            }
        }

        try
        {
            var transaction = await transactionQueryService
                .GetByIdAsync(cardNumber, transactionId, targetCurrency, cancellationToken)
                .ConfigureAwait(false);

            return Results.Ok(MapTransaction(transaction));
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static Dictionary<string, string[]> ValidatePurchaseRequest(PurchaseApiRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (!IsValidPan(request.CardNumber))
        {
            errors["cardNumber"] = ["Card number must be exactly 16 numeric digits."];
        }

        try
        {
            _ = Cvv.Create(request.Cvv);
        }
        catch (ArgumentException ex)
        {
            errors["cvv"] = [ex.Message];
        }

        try
        {
            _ = CurrencyCode.Create(request.Currency);
        }
        catch (ArgumentException ex)
        {
            errors["currency"] = [ex.Message];
        }

        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Length > 200)
        {
            errors["description"] = ["Description is required and must be at most 200 characters."];
        }

        return errors;
    }

    private static bool IsValidPan(string cardNumber)
    {
        try
        {
            _ = Pan.Create(cardNumber);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static TransactionDetailApiResponse MapTransaction(TransactionDetailDto dto) =>
        new(
            dto.Id,
            dto.Description,
            dto.TransactionDate,
            CardEndpoints.FormatDecimal(dto.Amount),
            dto.Currency,
            dto.ConvertedAmount is null ? null : CardEndpoints.FormatDecimal(dto.ConvertedAmount.Value),
            dto.ConvertedCurrency,
            dto.RateUsed is null ? null : CardEndpoints.FormatDecimal(dto.RateUsed.Value),
            dto.RateDate);

    private sealed record PurchaseApiRequest(
        string CardNumber,
        DateOnly ExpiryDate,
        string Cvv,
        string Amount,
        string Currency,
        string Description);

    private sealed record PurchaseApiResponse(
        Guid Id,
        string Amount,
        string Currency,
        string Description);

    private sealed record TransactionDetailApiResponse(
        Guid Id,
        string Description,
        DateTimeOffset TransactionDate,
        string Amount,
        string Currency,
        string? ConvertedAmount,
        string? ConvertedCurrency,
        string? RateUsed,
        DateOnly? RateDate);
}

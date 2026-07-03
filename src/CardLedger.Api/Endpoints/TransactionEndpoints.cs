using System.Globalization;
using CardLedger.Api.Contracts;
using CardLedger.Api.Validation;
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
            .WithSummary("Record a purchase")
            .WithDescription(
                "Validates card credentials, records a transaction, and debits the card ledger. " +
                "Cross-currency purchases are converted using the latest Treasury rate.")
            .WithTags("Transactions")
            .Accepts<PurchaseApiRequest>("application/json")
            .Produces<PurchaseApiResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/{cardNumber}/transactions", ListTransactionsAsync)
            .WithName("ListTransactions")
            .WithSummary("List transactions for a card")
            .WithDescription(
                "Returns all transactions for the card. When targetCurrency is provided and " +
                "differs from each transaction's currency, converted fields use Treasury rates " +
                "within a 6-month lookback window.")
            .WithTags("Transactions")
            .Produces<IReadOnlyList<TransactionDetailApiResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/{cardNumber}/transactions/{transactionId:guid}", GetTransactionAsync)
            .WithName("GetTransaction")
            .WithSummary("Get a single transaction")
            .WithDescription(
                "Returns one transaction by identifier. When targetCurrency is provided and " +
                "differs from the transaction currency, converted fields use the same 6-month " +
                "Treasury lookback as the list endpoint.")
            .WithTags("Transactions")
            .Produces<TransactionDetailApiResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return group;
    }

    private static async Task<IResult> PurchaseAsync(
        PurchaseApiRequest request,
        PurchaseService purchaseService,
        CurrencyValidator currencyValidator,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidatePurchaseRequest(request, currencyValidator);
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
                    ["targetCurrency"] = [ApiValidationMessages.ForCurrency(targetCurrency, ex)]
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
                    ["targetCurrency"] = [ApiValidationMessages.ForCurrency(targetCurrency, ex)]
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

    private static Dictionary<string, string[]> ValidatePurchaseRequest(
        PurchaseApiRequest request,
        CurrencyValidator currencyValidator)
    {
        var errors = new Dictionary<string, string[]>();

        if (!IsValidPan(request.CardNumber))
        {
            errors["cardNumber"] = ["Card number must be exactly 16 numeric digits."];
        }

        try
        {
            _ = CardExpiry.Parse(request.ExpiryDate);
        }
        catch (ArgumentException ex)
        {
            errors["expiryDate"] = [ApiValidationMessages.ForExpiry(request.ExpiryDate, ex)];
        }

        try
        {
            _ = Cvv.Create(request.Cvv);
        }
        catch (ArgumentException ex)
        {
            errors["cvv"] = [ApiValidationMessages.ForCvv(ex)];
        }

        try
        {
            currencyValidator.ValidateSupported(request.Currency);
        }
        catch (ArgumentException ex)
        {
            errors["currency"] = [ApiValidationMessages.ForCurrency(request.Currency, ex)];
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
            dto.RateUsed is null ? null : CardEndpoints.FormatRate(dto.RateUsed.Value),
            dto.RateDate);
}

using CardLedger.Api.Contracts;
using CardLedger.Api.Validation;
using CardLedger.Application.Services;
using CardLedger.Domain.ValueObjects;

namespace CardLedger.Api.Endpoints;

public static class BalanceEndpoints
{
    public static RouteGroupBuilder MapBalanceEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{cardNumber}/balance", GetBalanceAsync)
            .WithName("GetAvailableBalance")
            .WithSummary("Get available balance")
            .WithDescription(
                "Returns ledger-stored available balance. When targetCurrency is omitted, " +
                "returns the balance in the card's ledger currency. When provided, converts " +
                "using the latest cached Treasury exchange rate.")
            .WithTags("Balance")
            .Produces<BalanceApiResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return group;
    }

    private static async Task<IResult> GetBalanceAsync(
        string cardNumber,
        string? targetCurrency,
        BalanceService balanceService,
        CancellationToken cancellationToken)
    {
        if (!IsValidPan(cardNumber))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["cardNumber"] = ["Card number must be exactly 16 numeric digits."]
            });
        }

        if (!string.IsNullOrWhiteSpace(targetCurrency))
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

        var response = await balanceService
            .GetBalanceAsync(cardNumber, targetCurrency, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(new BalanceApiResponse(
            CardEndpoints.FormatDecimal(response.AvailableBalance),
            response.Currency,
            response.RateUsed is null ? null : CardEndpoints.FormatDecimal(response.RateUsed.Value),
            response.RateDate));
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
}

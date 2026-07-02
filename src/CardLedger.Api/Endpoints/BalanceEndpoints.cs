using CardLedger.Application.Services;
using CardLedger.Domain.ValueObjects;

namespace CardLedger.Api.Endpoints;

public static class BalanceEndpoints
{
    public static RouteGroupBuilder MapBalanceEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{cardNumber}/balance", GetBalanceAsync)
            .WithName("GetAvailableBalance")
            .Produces<BalanceApiResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return group;
    }

    private static async Task<IResult> GetBalanceAsync(
        string cardNumber,
        string targetCurrency,
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

    private sealed record BalanceApiResponse(
        string AvailableBalance,
        string Currency,
        string? RateUsed,
        DateOnly? RateDate);
}

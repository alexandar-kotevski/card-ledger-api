using System.Globalization;
using CardLedger.Application.DTOs;
using CardLedger.Application.Services;
using CardLedger.Domain.ValueObjects;

namespace CardLedger.Api.Endpoints;

public static class CardEndpoints
{
    public static RouteGroupBuilder MapCardEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", IssueCardAsync)
            .WithName("IssueCard")
            .Produces<IssueCardResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return group;
    }

    private static async Task<IResult> IssueCardAsync(
        IssueCardApiRequest request,
        IssueCardService issueCardService,
        CancellationToken cancellationToken)
    {
        if (!TryParseDecimal(request.CreditLimit, out var creditLimit))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["creditLimit"] = ["Credit limit must be a valid decimal string."]
            });
        }

        try
        {
            _ = CurrencyCode.Create(request.Currency);
        }
        catch (ArgumentException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["currency"] = [ex.Message]
            });
        }

        try
        {
            var response = await issueCardService
                .IssueAsync(new IssueCardRequest(creditLimit, request.Currency), cancellationToken)
                .ConfigureAwait(false);

            return Results.Created(
                $"/api/cards/{response.CardNumber}",
                new IssueCardApiResponse(
                    response.CardNumber,
                    response.ExpiryDate,
                    response.Cvv,
                    response.Currency,
                    FormatDecimal(response.CreditLimit)));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [ex.ParamName ?? "creditLimit"] = [ex.Message]
            });
        }
    }

    internal static bool TryParseDecimal(string? value, out decimal result) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);

    internal static string FormatDecimal(decimal value) =>
        value.ToString("0.####", CultureInfo.InvariantCulture);

    private sealed record IssueCardApiRequest(string CreditLimit, string Currency);

    private sealed record IssueCardApiResponse(
        string CardNumber,
        DateOnly ExpiryDate,
        string Cvv,
        string Currency,
        string CreditLimit);
}

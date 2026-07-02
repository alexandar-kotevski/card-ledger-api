using CardLedger.Api.Endpoints;
using CardLedger.Application;
using CardLedger.Domain.Exceptions;
using CardLedger.Infrastructure.DependencyInjection;
using CardLedger.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCardLedgerApplication();
builder.Services.AddCardLedgerInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<CardLedgerDbContext>();

var app = builder.Build();

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        if (exception is null)
        {
            return;
        }

        var (statusCode, title, extensions) = MapException(exception);
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        foreach (var (key, value) in extensions)
        {
            problem.Extensions[key] = value;
        }

        await context.Response.WriteAsJsonAsync(problem).ConfigureAwait(false);
    });
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CardLedgerDbContext>();
    var migrationsAssembly = typeof(CardLedgerDbContext).Assembly;
    var hasMigrations = migrationsAssembly
        .GetManifestResourceNames()
        .Any(n => n.Contains("Migrations", StringComparison.Ordinal))
        || Directory.Exists(Path.Combine(AppContext.BaseDirectory, "Migrations"))
        || db.Database.GetMigrations().Any();

    if (hasMigrations)
    {
        await db.Database.MigrateAsync().ConfigureAwait(false);
    }
    else
    {
        await db.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }
}

app.MapHealthChecks("/health");

var cards = app.MapGroup("/api/cards");
cards.MapCardEndpoints();
cards.MapTransactionEndpoints();
cards.MapBalanceEndpoints();

await app.RunAsync().ConfigureAwait(false);

static (int StatusCode, string Title, Dictionary<string, object?> Extensions) MapException(Exception exception) =>
    exception switch
    {
        ExchangeRateNotFoundException ex => (
            StatusCodes.Status422UnprocessableEntity,
            "Exchange rate not found",
            new Dictionary<string, object?>
            {
                ["cardNumber"] = ex.CardNumber,
                ["transactionId"] = ex.TransactionId,
                ["sourceCurrency"] = ex.SourceCurrency,
                ["targetCurrency"] = ex.TargetCurrency,
                ["transactionDate"] = ex.TransactionDate
            }),
        InsufficientBalanceException ex => (
            StatusCodes.Status422UnprocessableEntity,
            "Insufficient balance",
            new Dictionary<string, object?>
            {
                ["cardNumber"] = ex.CardNumber,
                ["requestedAmount"] = ex.RequestedAmount,
                ["availableBalance"] = ex.AvailableBalance
            }),
        InvalidCardCredentialsException => (
            StatusCodes.Status422UnprocessableEntity,
            "Invalid card credentials",
            []),
        CardExpiredException ex => (
            StatusCodes.Status422UnprocessableEntity,
            "Card expired",
            new Dictionary<string, object?>
            {
                ["cardNumber"] = ex.CardNumber,
                ["expiryDate"] = ex.ExpiryDate
            }),
        CardNotFoundException ex => (
            StatusCodes.Status404NotFound,
            "Card not found",
            new Dictionary<string, object?>
            {
                ["cardNumber"] = ex.CardNumber
            }),
        KeyNotFoundException => (
            StatusCodes.Status404NotFound,
            "Resource not found",
            []),
        ArgumentException => (
            StatusCodes.Status400BadRequest,
            "Validation error",
            []),
        _ => (
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred",
            [])
    };

public partial class Program;

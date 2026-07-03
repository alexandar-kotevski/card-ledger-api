using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace CardLedger.Api.OpenApi;

internal static class OpenApiConfiguration
{
    public static IServiceCollection AddCardLedgerOpenApi(this IServiceCollection services) =>
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "Card Ledger API",
                    Version = "v1",
                    Description =
                        "Card issuance, purchase, transaction retrieval with Treasury FX conversion, " +
                        "and ledger-backed available balance queries. " +
                        "Monetary amounts are serialised as decimal strings in JSON."
                };

                return Task.CompletedTask;
            });
        });

    public static WebApplication MapCardLedgerOpenApi(this WebApplication app)
    {
        var openApiEnabled = app.Configuration
            .GetSection(OpenApiOptions.SectionName)
            .GetValue<bool>(nameof(OpenApiOptions.Enabled));

        if (!app.Environment.IsDevelopment() && !openApiEnabled)
        {
            return app;
        }

        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("Card Ledger API");
        });

        return app;
    }
}

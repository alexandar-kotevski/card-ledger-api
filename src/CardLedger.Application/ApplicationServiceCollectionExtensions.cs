using CardLedger.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CardLedger.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddCardLedgerApplication(this IServiceCollection services)
    {
        services.AddSingleton<CurrencyValidator>();
        services.AddScoped<IssueCardService>();
        services.AddScoped<PurchaseService>();
        services.AddScoped<CurrencyConversionService>();
        services.AddScoped<ExchangeRateLookbackService>();
        services.AddScoped<TransactionQueryService>();
        services.AddScoped<BalanceService>();

        return services;
    }
}

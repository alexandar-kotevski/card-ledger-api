using CardLedger.Application.Abstractions;
using CardLedger.Infrastructure.Persistence;
using CardLedger.Infrastructure.Persistence.Repositories;
using CardLedger.Infrastructure.Treasury;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CardLedger.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCardLedgerInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<CardLedgerDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.Configure<TreasurySyncOptions>(configuration.GetSection(TreasurySyncOptions.SectionName));

        services.AddSingleton<TreasurySyncState>();
        services.AddSingleton<ITreasurySyncState>(sp => sp.GetRequiredService<TreasurySyncState>());

        services.AddHttpClient<ITreasuryRateClient, TreasuryRateClient>();

        services.AddScoped<ICardRepository, CardRepository>();
        services.AddScoped<ILedgerRepository, LedgerRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IExchangeRateRepository, ExchangeRateRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddHostedService<TreasuryRateSyncService>();

        return services;
    }
}

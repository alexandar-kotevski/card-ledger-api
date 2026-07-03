using CardLedger.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CardLedger.Integration.Tests;

public sealed class CardLedgerWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public CardLedgerWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["TreasurySync:Enabled"] = "false",
                ["TreasurySync:SupportedCurrencyFromDate"] = "2025-12-31"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<CardLedgerDbContext>));
            services.AddDbContext<CardLedgerDbContext>(options =>
                options.UseNpgsql(_connectionString));
            services.AddHostedService<IntegrationTestCurrencyBootstrap>();
        });
    }
}

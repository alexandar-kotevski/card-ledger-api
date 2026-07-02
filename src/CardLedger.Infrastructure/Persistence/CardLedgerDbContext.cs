using CardLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CardLedger.Infrastructure.Persistence;

public sealed class CardLedgerDbContext : DbContext
{
    public CardLedgerDbContext(DbContextOptions<CardLedgerDbContext> options)
        : base(options)
    {
    }

    public DbSet<Card> Cards => Set<Card>();

    public DbSet<Ledger> Ledgers => Set<Ledger>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CardLedgerDbContext).Assembly);
    }
}

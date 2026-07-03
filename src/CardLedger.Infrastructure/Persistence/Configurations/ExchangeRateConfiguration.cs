using CardLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CardLedger.Infrastructure.Persistence.Configurations;

internal sealed class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        builder.ToTable("exchange_rates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CountryCurrencyDesc).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Rate).HasPrecision(18, 8);
        builder.Property(x => x.EffectiveDate).IsRequired();
        builder.Property(x => x.RecordDate).IsRequired();
        builder.HasIndex(x => new { x.CurrencyCode, x.EffectiveDate }).IsUnique();
        builder.HasIndex(x => x.RecordDate);
    }
}

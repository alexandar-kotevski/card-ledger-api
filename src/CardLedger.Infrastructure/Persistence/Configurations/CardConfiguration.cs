using CardLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CardLedger.Infrastructure.Persistence.Configurations;

internal sealed class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.ToTable("cards");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Pan).HasMaxLength(16).IsRequired();
        builder.HasIndex(x => x.Pan).IsUnique();
        builder.Property(x => x.CvvHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.CreditLimit).HasPrecision(18, 4);
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.IssuedAt).IsRequired();

        builder.HasOne(x => x.Ledger)
            .WithOne(x => x.Card)
            .HasForeignKey<Ledger>(x => x.CardId);
    }
}

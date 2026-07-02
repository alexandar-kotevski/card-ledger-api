namespace CardLedger.Domain.Entities;

public class Ledger
{
    public Guid Id { get; set; }

    public Guid CardId { get; set; }

    public decimal AvailableBalance { get; set; }

    public required string Currency { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Card? Card { get; set; }
}

namespace CardLedger.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; }

    public Guid CardId { get; set; }

    public required string Description { get; set; }

    public DateTimeOffset TransactionDate { get; set; }

    public decimal Amount { get; set; }

    public required string Currency { get; set; }

    public Card? Card { get; set; }
}

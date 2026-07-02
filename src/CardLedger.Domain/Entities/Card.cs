namespace CardLedger.Domain.Entities;

public class Card
{
    public Guid Id { get; set; }

    public required string Pan { get; set; }

    public DateOnly ExpiryDate { get; set; }

    public required string CvvHash { get; set; }

    public decimal CreditLimit { get; set; }

    public required string Currency { get; set; }

    public DateTimeOffset IssuedAt { get; set; }

    public Ledger? Ledger { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = [];
}

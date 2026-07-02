namespace CardLedger.Domain.Exceptions;

public class CardExpiredException : Exception
{
    public string CardNumber { get; }

    public DateOnly ExpiryDate { get; }

    public CardExpiredException(string cardNumber, DateOnly expiryDate)
        : base($"Card {cardNumber} expired on {expiryDate:yyyy-MM-dd}.")
    {
        CardNumber = cardNumber;
        ExpiryDate = expiryDate;
    }
}

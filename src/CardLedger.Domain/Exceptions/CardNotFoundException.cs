namespace CardLedger.Domain.Exceptions;

public class CardNotFoundException : Exception
{
    public string CardNumber { get; }

    public CardNotFoundException(string cardNumber)
        : base($"Card {cardNumber} was not found.")
    {
        CardNumber = cardNumber;
    }
}

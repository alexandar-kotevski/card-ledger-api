namespace CardLedger.Domain.Exceptions;

public class InsufficientBalanceException : Exception
{
    public string CardNumber { get; }

    public decimal RequestedAmount { get; }

    public decimal AvailableBalance { get; }

    public InsufficientBalanceException(string cardNumber, decimal requestedAmount, decimal availableBalance)
        : base($"Insufficient balance for card {cardNumber}. Requested {requestedAmount}, available {availableBalance}.")
    {
        CardNumber = cardNumber;
        RequestedAmount = requestedAmount;
        AvailableBalance = availableBalance;
    }
}

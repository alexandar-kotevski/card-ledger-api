namespace CardLedger.Domain.Exceptions;

public class InvalidCardCredentialsException : Exception
{
    public InvalidCardCredentialsException()
        : base("Invalid card credentials.")
    {
    }
}

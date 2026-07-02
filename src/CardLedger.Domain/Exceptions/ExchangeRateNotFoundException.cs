namespace CardLedger.Domain.Exceptions;

public class ExchangeRateNotFoundException : Exception
{
    public string? CardNumber { get; }

    public Guid? TransactionId { get; }

    public string SourceCurrency { get; }

    public string TargetCurrency { get; }

    public DateTimeOffset? TransactionDate { get; }

    public ExchangeRateNotFoundException(
        string sourceCurrency,
        string targetCurrency,
        string? cardNumber = null,
        Guid? transactionId = null,
        DateTimeOffset? transactionDate = null)
        : base(BuildMessage(sourceCurrency, targetCurrency, cardNumber, transactionId, transactionDate))
    {
        SourceCurrency = sourceCurrency;
        TargetCurrency = targetCurrency;
        CardNumber = cardNumber;
        TransactionId = transactionId;
        TransactionDate = transactionDate;
    }

    private static string BuildMessage(
        string sourceCurrency,
        string targetCurrency,
        string? cardNumber,
        Guid? transactionId,
        DateTimeOffset? transactionDate)
    {
        var parts = new List<string>
        {
            $"No exchange rate found for {sourceCurrency} to {targetCurrency}."
        };

        if (cardNumber is not null)
        {
            parts.Add($"Card: {cardNumber}.");
        }

        if (transactionId is not null)
        {
            parts.Add($"Transaction: {transactionId}.");
        }

        if (transactionDate is not null)
        {
            parts.Add($"Transaction date: {transactionDate:O}.");
        }

        return string.Join(' ', parts);
    }
}

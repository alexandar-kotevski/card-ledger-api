namespace CardLedger.Domain.ValueObjects;

public sealed record Money(decimal Amount, CurrencyCode Currency)
{
    public static Money Create(decimal amount, string currencyCode)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");
        }

        return new Money(amount, CurrencyCode.Create(currencyCode));
    }

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return this with { Amount = Amount + other.Amount };
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return this with { Amount = Amount - other.Amount };
    }

    private void EnsureSameCurrency(Money other)
    {
        if (!Currency.Equals(other.Currency))
        {
            throw new InvalidOperationException(
                $"Cannot combine amounts in {Currency.Value} and {other.Currency.Value}.");
        }
    }
}

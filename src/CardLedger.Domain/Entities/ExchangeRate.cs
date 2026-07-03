namespace CardLedger.Domain.Entities;

public class ExchangeRate
{
    public long Id { get; set; }

    public required string CountryCurrencyDesc { get; set; }

    public required string CurrencyCode { get; set; }

    public decimal Rate { get; set; }

    public DateOnly EffectiveDate { get; set; }
}

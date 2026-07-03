using CardLedger.Application.Abstractions;
using CardLedger.Domain.ValueObjects;

namespace CardLedger.Application.Services;

public sealed class CurrencyValidator
{
    private readonly ISupportedCurrencyCache _supportedCurrencyCache;

    public CurrencyValidator(ISupportedCurrencyCache supportedCurrencyCache)
    {
        _supportedCurrencyCache = supportedCurrencyCache;
    }

    public CurrencyCode ValidateSupported(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Currency code is required.");
        }

        var normalized = code.Trim().ToUpperInvariant();
        if (!CurrencyCode.IsValidFormat(normalized))
        {
            throw new ArgumentException($"Invalid ISO 4217 currency code: {code}.");
        }

        if (!_supportedCurrencyCache.IsSupportedIso(normalized))
        {
            throw new ArgumentException($"Unsupported currency code: {code}.");
        }

        return CurrencyCode.Create(normalized);
    }
}

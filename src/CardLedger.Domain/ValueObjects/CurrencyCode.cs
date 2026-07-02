using System.Text.RegularExpressions;

namespace CardLedger.Domain.ValueObjects;

public sealed partial class CurrencyCode : IEquatable<CurrencyCode>
{
    private static readonly HashSet<string> SupportedCodes =
    [
        "USD", "EUR", "GBP", "AUD", "CAD", "JPY"
    ];

    public string Value { get; }

    private CurrencyCode(string value) => Value = value;

    public static CurrencyCode Create(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Currency code is required.", nameof(code));
        }

        var normalized = code.Trim().ToUpperInvariant();
        if (!Iso4217Pattern().IsMatch(normalized))
        {
            throw new ArgumentException($"Invalid ISO 4217 currency code: {code}.", nameof(code));
        }

        if (!SupportedCodes.Contains(normalized))
        {
            throw new ArgumentException($"Unsupported currency code: {code}.", nameof(code));
        }

        return new CurrencyCode(normalized);
    }

    public static bool IsSupported(string code) =>
        !string.IsNullOrWhiteSpace(code) &&
        Iso4217Pattern().IsMatch(code.Trim().ToUpperInvariant()) &&
        SupportedCodes.Contains(code.Trim().ToUpperInvariant());

    public bool Equals(CurrencyCode? other) =>
        other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is CurrencyCode other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex Iso4217Pattern();
}

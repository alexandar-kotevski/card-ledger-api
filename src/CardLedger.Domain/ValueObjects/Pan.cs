using System.Text.RegularExpressions;

namespace CardLedger.Domain.ValueObjects;

public sealed partial class Pan : IEquatable<Pan>
{
    public string Value { get; }

    private Pan(string value) => Value = value;

    public static Pan Create(string pan)
    {
        if (string.IsNullOrWhiteSpace(pan))
        {
            throw new ArgumentException("Card number is required.", nameof(pan));
        }

        var normalized = pan.Trim();
        if (!PanPattern().IsMatch(normalized))
        {
            throw new ArgumentException("Card number must be exactly 16 numeric digits.", nameof(pan));
        }

        return new Pan(normalized);
    }

    public bool Equals(Pan? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is Pan other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;

    [GeneratedRegex(@"^\d{16}$")]
    private static partial Regex PanPattern();
}

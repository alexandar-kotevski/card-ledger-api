using System.Text.RegularExpressions;

namespace CardLedger.Domain.ValueObjects;

public sealed partial class Cvv : IEquatable<Cvv>
{
    public string Value { get; }

    private Cvv(string value) => Value = value;

    public static Cvv Create(string cvv)
    {
        if (string.IsNullOrWhiteSpace(cvv))
        {
            throw new ArgumentException("CVV is required.", nameof(cvv));
        }

        var normalized = cvv.Trim();
        if (!CvvPattern().IsMatch(normalized))
        {
            throw new ArgumentException("CVV must be exactly 3 numeric digits.", nameof(cvv));
        }

        return new Cvv(normalized);
    }

    public bool Equals(Cvv? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is Cvv other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;

    [GeneratedRegex(@"^\d{3}$")]
    private static partial Regex CvvPattern();
}

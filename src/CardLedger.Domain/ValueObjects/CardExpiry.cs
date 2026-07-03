using System.Globalization;
using System.Text.RegularExpressions;

namespace CardLedger.Domain.ValueObjects;

public sealed partial class CardExpiry : IEquatable<CardExpiry>
{
    public DateOnly EndOfMonthDate { get; }

    public string MmYy { get; }

    private CardExpiry(DateOnly endOfMonthDate, string mmYy)
    {
        EndOfMonthDate = endOfMonthDate;
        MmYy = mmYy;
    }

    public static CardExpiry FromIssueDate(DateOnly issueDate, int yearsValid = 3)
    {
        var expiryMonth = issueDate.AddYears(yearsValid);
        var endOfMonth = ToEndOfMonth(expiryMonth.Year, expiryMonth.Month);
        return new CardExpiry(endOfMonth, FormatMmYy(endOfMonth));
    }

    public static CardExpiry Parse(string mmYy)
    {
        if (string.IsNullOrWhiteSpace(mmYy))
        {
            throw new ArgumentException("Expiry date is required.");
        }

        var normalized = mmYy.Trim();
        if (!MmYyPattern().IsMatch(normalized))
        {
            throw new ArgumentException("Expiry date must be in MM/YY format (e.g. 07/29).");
        }

        var parts = normalized.Split('/');
        var month = int.Parse(parts[0], CultureInfo.InvariantCulture);
        var year = 2000 + int.Parse(parts[1], CultureInfo.InvariantCulture);
        var endOfMonth = ToEndOfMonth(year, month);
        return new CardExpiry(endOfMonth, FormatMmYy(endOfMonth));
    }

    public static CardExpiry FromEndOfMonthDate(DateOnly endOfMonthDate) =>
        new(endOfMonthDate, FormatMmYy(endOfMonthDate));

    public bool IsExpired(DateOnly asOfDate) => EndOfMonthDate < asOfDate;

    public bool Equals(CardExpiry? other) =>
        other is not null && EndOfMonthDate == other.EndOfMonthDate;

    public override bool Equals(object? obj) => obj is CardExpiry other && Equals(other);

    public override int GetHashCode() => EndOfMonthDate.GetHashCode();

    public override string ToString() => MmYy;

    private static DateOnly ToEndOfMonth(int year, int month) =>
        new(year, month, DateTime.DaysInMonth(year, month));

    private static string FormatMmYy(DateOnly date) =>
        $"{date.Month:00}/{date.Year % 100:00}";

    [GeneratedRegex(@"^(0[1-9]|1[0-2])/\d{2}$")]
    private static partial Regex MmYyPattern();
}

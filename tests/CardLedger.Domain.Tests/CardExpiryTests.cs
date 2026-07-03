using CardLedger.Domain.ValueObjects;

namespace CardLedger.Domain.Tests;

public class CardExpiryTests
{
    [Fact]
    public void FromIssueDate_ReturnsEndOfMonthThreeYearsOut()
    {
        var expiry = CardExpiry.FromIssueDate(new DateOnly(2026, 7, 3));

        Assert.Equal("07/29", expiry.MmYy);
        Assert.Equal(new DateOnly(2029, 7, 31), expiry.EndOfMonthDate);
    }

    [Fact]
    public void Parse_AcceptsMmYyFormat()
    {
        var expiry = CardExpiry.Parse("07/29");

        Assert.Equal("07/29", expiry.MmYy);
        Assert.Equal(new DateOnly(2029, 7, 31), expiry.EndOfMonthDate);
    }

    [Fact]
    public void Parse_RejectsInvalidMonth()
    {
        var ex = Assert.Throws<ArgumentException>(() => CardExpiry.Parse("13/29"));
        Assert.DoesNotContain("Parameter", ex.Message);
    }

    [Fact]
    public void IsExpired_ReturnsTrueAfterEndOfMonth()
    {
        var expiry = CardExpiry.Parse("01/20");

        Assert.True(expiry.IsExpired(new DateOnly(2020, 2, 1)));
        Assert.False(expiry.IsExpired(new DateOnly(2020, 1, 31)));
    }
}

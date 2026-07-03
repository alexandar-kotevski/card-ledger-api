using CardLedger.Application.Abstractions;
using CardLedger.Application.Services;
using NSubstitute;

namespace CardLedger.Application.Tests;

public class CurrencyValidatorTests
{
    [Fact]
    public void ValidateSupported_AcceptsCachedIsoCode()
    {
        var cache = Substitute.For<ISupportedCurrencyCache>();
        cache.IsSupportedIso("INR").Returns(true);
        var validator = new CurrencyValidator(cache);

        var result = validator.ValidateSupported("INR");

        Assert.Equal("INR", result.Value);
    }

    [Fact]
    public void ValidateSupported_RejectsUnknownIsoCode()
    {
        var cache = Substitute.For<ISupportedCurrencyCache>();
        cache.IsSupportedIso(Arg.Any<string>()).Returns(false);
        var validator = new CurrencyValidator(cache);

        var ex = Assert.Throws<ArgumentException>(() => validator.ValidateSupported("XXX"));
        Assert.Equal("Unsupported currency code: XXX.", ex.Message);
        Assert.DoesNotContain("Parameter", ex.Message);
    }

    [Fact]
    public void ValidateSupported_RejectsMissingCodeWithoutParameterName()
    {
        var validator = TestCurrencySupport.CreateValidator("USD");

        var ex = Assert.Throws<ArgumentException>(() => validator.ValidateSupported(""));
        Assert.Equal("Currency code is required.", ex.Message);
    }
}

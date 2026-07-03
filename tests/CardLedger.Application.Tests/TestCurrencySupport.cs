using CardLedger.Application.Abstractions;
using CardLedger.Application.Services;
using NSubstitute;

namespace CardLedger.Application.Tests;

internal static class TestCurrencySupport
{
    public static CurrencyValidator CreateValidator(params string[] supportedCodes)
    {
        var cache = Substitute.For<ISupportedCurrencyCache>();
        cache.IsSupportedIso(Arg.Any<string>()).Returns(call =>
        {
            var code = call.Arg<string>().Trim().ToUpperInvariant();
            return supportedCodes.Contains(code, StringComparer.OrdinalIgnoreCase);
        });

        return new CurrencyValidator(cache);
    }
}

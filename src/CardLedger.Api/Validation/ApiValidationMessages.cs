namespace CardLedger.Api.Validation;

internal static class ApiValidationMessages
{
    public static string ForCurrency(string? code, Exception ex) =>
        ex switch
        {
            ArgumentException when string.IsNullOrWhiteSpace(code) =>
                "Currency code is required.",
            ArgumentException argumentException =>
                StripParameterSuffix(argumentException.Message),
            _ => "Currency code is invalid."
        };

    public static string ForExpiry(string? expiry, Exception ex) =>
        ex switch
        {
            ArgumentException when string.IsNullOrWhiteSpace(expiry) =>
                "Expiry date is required.",
            ArgumentException argumentException =>
                StripParameterSuffix(argumentException.Message),
            _ => "Expiry date is invalid."
        };

    public static string ForCvv(Exception ex) =>
        ex is ArgumentException argumentException
            ? StripParameterSuffix(argumentException.Message)
            : "CVV is invalid.";

    public static string ForCreditLimit(Exception ex) =>
        ex is ArgumentOutOfRangeException { Message: var message }
            ? StripParameterSuffix(message)
            : "Credit limit is invalid.";

    private static string StripParameterSuffix(string message)
    {
        const string suffix = " (Parameter";
        var index = message.IndexOf(suffix, StringComparison.Ordinal);
        return index >= 0 ? message[..index].TrimEnd('.') + "." : message;
    }
}

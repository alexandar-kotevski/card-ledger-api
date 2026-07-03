namespace CardLedger.Api.OpenApi;

public sealed class OpenApiOptions
{
    public const string SectionName = "OpenApi";

    /// <summary>
    /// When true, exposes <c>/openapi/v1.json</c> and Scalar at <c>/scalar/v1</c>.
    /// Enabled automatically in Development; set explicitly for Docker/Production.
    /// </summary>
    public bool Enabled { get; set; }
}

using MiniBanking.SharedKernel.Results;

namespace MiniBanking.SharedKernel.Monetary;

public static class CurrencyErrors
{
    public static readonly Error Required =
        Error.Validation("Currency.Required", "Currency code is required.");

    public static Error Unsupported(string code) =>
        Error.Validation("Currency.Unsupported", $"Currency '{code}' is not supported.");
}

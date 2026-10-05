using MiniBanking.SharedKernel.Results;

namespace MiniBanking.SharedKernel.Monetary;

/// <summary>
/// An ISO 4217 currency supported by the bank.
/// <see cref="MinorUnits"/> is the number of decimal places the currency uses
/// (TRY/USD/EUR/GBP: 2 — kuruş/cents; JPY: 0).
/// </summary>
public sealed record Currency
{
    public static readonly Currency TRY = new("TRY", 2);
    public static readonly Currency USD = new("USD", 2);
    public static readonly Currency EUR = new("EUR", 2);
    public static readonly Currency GBP = new("GBP", 2);
    public static readonly Currency JPY = new("JPY", 0);

    private static readonly Dictionary<string, Currency> Supported =
        new[] { TRY, USD, EUR, GBP, JPY }.ToDictionary(currency => currency.Code);

    private Currency(string code, int minorUnits)
    {
        Code = code;
        MinorUnits = minorUnits;
    }

    public string Code { get; }

    public int MinorUnits { get; }

    public static IReadOnlyCollection<Currency> All => Supported.Values;

    public static Result<Currency> Create(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return CurrencyErrors.Required;

        var normalized = code.Trim().ToUpperInvariant();

        return Supported.TryGetValue(normalized, out var currency)
            ? currency
            : CurrencyErrors.Unsupported(normalized);
    }

    public override string ToString() => Code;
}

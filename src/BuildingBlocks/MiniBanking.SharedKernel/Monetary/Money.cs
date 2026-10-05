using System.Globalization;
using MiniBanking.SharedKernel.Results;

namespace MiniBanking.SharedKernel.Monetary;

/// <summary>
/// An amount in a specific currency. Banking rules enforced here:
/// <list type="bullet">
/// <item>always <see cref="decimal"/>, never double/float;</item>
/// <item>never more decimal places than the currency's minor units;</item>
/// <item>never mixing currencies implicitly (conversion is an explicit FX operation);</item>
/// <item>never rounding implicitly: calculations take an explicit <see cref="MidpointRounding"/>.</item>
/// </list>
/// </summary>
public sealed record Money
{
    private Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public Currency Currency { get; }

    public bool IsZero => Amount == 0m;

    public bool IsPositive => Amount > 0m;

    public bool IsNegative => Amount < 0m;

    public static Result<Money> Create(decimal amount, Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);

        if (decimal.Round(amount, currency.MinorUnits) != amount)
            return MoneyErrors.InvalidPrecision(currency);

        return new Money(amount, currency);
    }

    public static Money Zero(Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);
        return new Money(0m, currency);
    }

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    /// <summary>
    /// Multiplies by a factor (rate, percentage, ...) and rounds the result to the currency's
    /// minor units using the given rule. The caller must choose the rule explicitly.
    /// </summary>
    public Money Multiply(decimal factor, MidpointRounding rounding) =>
        new(decimal.Round(Amount * factor, Currency.MinorUnits, rounding), Currency);

    /// <summary>
    /// Splits the amount into <paramref name="parts"/> shares that differ by at most one minor unit
    /// and always add up to the original amount (no kuruş is lost or created).
    /// </summary>
    public IReadOnlyList<Money> Allocate(int parts)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(parts);

        var minorUnitFactor = MinorUnitFactor(Currency);
        var totalInMinorUnits = Amount * minorUnitFactor;

        var share = decimal.Truncate(totalInMinorUnits / parts);
        var remainder = totalInMinorUnits - share * parts;
        var step = Math.Sign(remainder);

        var result = new List<Money>(parts);
        for (var i = 0; i < parts; i++)
        {
            var shareInMinorUnits = i < Math.Abs(remainder) ? share + step : share;
            result.Add(new Money(shareInMinorUnits / minorUnitFactor, Currency));
        }

        return result;
    }

    public static Money operator +(Money left, Money right) => left.Add(right);

    public static Money operator -(Money left, Money right) => left.Subtract(right);

    public static bool operator >(Money left, Money right) => Compare(left, right) > 0;

    public static bool operator <(Money left, Money right) => Compare(left, right) < 0;

    public static bool operator >=(Money left, Money right) => Compare(left, right) >= 0;

    public static bool operator <=(Money left, Money right) => Compare(left, right) <= 0;

    public override string ToString() =>
        $"{Amount.ToString($"F{Currency.MinorUnits}", CultureInfo.InvariantCulture)} {Currency.Code}";

    private static int Compare(Money left, Money right)
    {
        left.EnsureSameCurrency(right);
        return left.Amount.CompareTo(right.Amount);
    }

    private static decimal MinorUnitFactor(Currency currency)
    {
        var factor = 1m;
        for (var i = 0; i < currency.MinorUnits; i++)
            factor *= 10m;

        return factor;
    }

    private void EnsureSameCurrency(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (Currency != other.Currency)
            throw new InvalidOperationException(
                $"Cannot combine {Currency.Code} with {other.Currency.Code}. Convert explicitly via FX.");
    }
}

using MiniBanking.SharedKernel.Results;

namespace MiniBanking.SharedKernel.Monetary;

public static class MoneyErrors
{
    public static Error InvalidPrecision(Currency currency) =>
        Error.Validation(
            "Money.InvalidPrecision",
            $"{currency.Code} amounts can have at most {currency.MinorUnits} decimal places.");
}

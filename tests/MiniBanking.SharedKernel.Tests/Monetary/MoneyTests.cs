using MiniBanking.SharedKernel.Monetary;

namespace MiniBanking.SharedKernel.Tests.Monetary;

public class MoneyTests
{
    private static Money Try(decimal amount) => Money.Create(amount, Currency.TRY).Value;

    private static Money Usd(decimal amount) => Money.Create(amount, Currency.USD).Value;

    [Fact]
    public void Decimal_is_exact_where_double_is_not()
    {
        (0.1 + 0.2).Should().NotBe(0.3);    // binary floating point: 0.30000000000000004
        (0.1m + 0.2m).Should().Be(0.3m);    // decimal: exact
    }

    public static TheoryData<decimal, string> ValidAmounts => new()
    {
        { 100m, "TRY" },
        { 100.5m, "TRY" },
        { 100.55m, "TRY" },
        { -20.10m, "USD" },
        { 1500m, "JPY" }
    };

    [Theory]
    [MemberData(nameof(ValidAmounts))]
    public void Create_accepts_amounts_within_the_currency_precision(decimal amount, string code)
    {
        var result = Money.Create(amount, Currency.Create(code).Value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(amount);
    }

    public static TheoryData<decimal, string> AmountsWithExcessPrecision => new()
    {
        { 100.555m, "TRY" },
        { 0.001m, "USD" },
        { 1500.5m, "JPY" }
    };

    [Theory]
    [MemberData(nameof(AmountsWithExcessPrecision))]
    public void Create_rejects_amounts_with_more_decimals_than_the_currency_allows(decimal amount, string code)
    {
        var result = Money.Create(amount, Currency.Create(code).Value);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Money.InvalidPrecision");
    }

    [Fact]
    public void Money_with_same_amount_and_currency_is_equal()
    {
        Try(100m).Should().Be(Try(100.00m));
    }

    [Fact]
    public void Money_with_same_amount_but_different_currency_is_not_equal()
    {
        Try(100m).Should().NotBe(Usd(100m));
    }

    [Fact]
    public void Adding_and_subtracting_same_currency_is_exact()
    {
        (Try(0.10m) + Try(0.20m)).Should().Be(Try(0.30m));
        (Try(100m) - Try(150.25m)).Should().Be(Try(-50.25m));
    }

    [Fact]
    public void Combining_different_currencies_throws()
    {
        var add = () => Try(10m) + Usd(10m);
        var compare = () => Try(10m) > Usd(10m);

        add.Should().Throw<InvalidOperationException>();
        compare.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Comparison_operators_work_within_a_currency()
    {
        var balance = Try(100m);
        var withdrawal = Try(150m);

        (withdrawal > balance).Should().BeTrue();
        (balance <= withdrawal).Should().BeTrue();
        (balance - withdrawal).IsNegative.Should().BeTrue();
    }

    public static TheoryData<decimal, decimal, MidpointRounding, decimal> Multiplications => new()
    {
        // 100.00 * 0.00125 = 0.125 -> exactly halfway between 0.12 and 0.13
        { 100m, 0.00125m, MidpointRounding.ToEven, 0.12m },
        { 100m, 0.00125m, MidpointRounding.AwayFromZero, 0.13m },
        // 100.00 * 0.00135 = 0.135 -> halfway between 0.13 and 0.14
        { 100m, 0.00135m, MidpointRounding.ToEven, 0.14m },
        { 100m, 0.00135m, MidpointRounding.AwayFromZero, 0.14m },
        // not a midpoint: both rules agree
        { 100m, 0.00126m, MidpointRounding.ToEven, 0.13m }
    };

    [Theory]
    [MemberData(nameof(Multiplications))]
    public void Multiply_rounds_to_minor_units_with_the_given_rule(
        decimal amount, decimal factor, MidpointRounding rounding, decimal expected)
    {
        Try(amount).Multiply(factor, rounding).Should().Be(Try(expected));
    }

    [Fact]
    public void Allocate_splits_without_losing_a_single_kurus()
    {
        var shares = Try(100m).Allocate(3);

        shares.Should().Equal(Try(33.34m), Try(33.33m), Try(33.33m));
        shares.Aggregate((sum, share) => sum + share).Should().Be(Try(100m));
    }

    [Fact]
    public void Allocate_works_for_negative_amounts()
    {
        var shares = Try(-0.05m).Allocate(2);

        shares.Should().Equal(Try(-0.03m), Try(-0.02m));
    }

    [Fact]
    public void Allocate_rejects_non_positive_parts()
    {
        var act = () => Try(100m).Allocate(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(1234.5, "TRY", "1234.50 TRY")]
    [InlineData(1500, "JPY", "1500 JPY")]
    public void ToString_uses_invariant_culture_and_minor_units(double amount, string code, string expected)
    {
        var money = Money.Create((decimal)amount, Currency.Create(code).Value).Value;

        money.ToString().Should().Be(expected);
    }
}

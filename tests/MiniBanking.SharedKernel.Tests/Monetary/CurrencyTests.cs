using MiniBanking.SharedKernel.Monetary;

namespace MiniBanking.SharedKernel.Tests.Monetary;

public class CurrencyTests
{
    [Theory]
    [InlineData("TRY")]
    [InlineData("try")]
    [InlineData(" usd ")]
    public void Create_accepts_supported_codes_case_insensitively(string code)
    {
        var result = Currency.Create(code);

        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be(code.Trim().ToUpperInvariant());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_missing_code(string? code)
    {
        var result = Currency.Create(code);

        result.Error.Should().Be(CurrencyErrors.Required);
    }

    [Fact]
    public void Create_rejects_unsupported_code()
    {
        var result = Currency.Create("XYZ");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Currency.Unsupported");
    }

    [Fact]
    public void Created_currency_is_the_same_as_the_predefined_one()
    {
        Currency.Create("try").Value.Should().Be(Currency.TRY);
    }

    [Fact]
    public void Yen_has_no_minor_units()
    {
        Currency.JPY.MinorUnits.Should().Be(0);
    }
}

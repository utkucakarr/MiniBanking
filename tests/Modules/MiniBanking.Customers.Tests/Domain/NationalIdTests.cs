using MiniBanking.Customers.Domain;

namespace MiniBanking.Customers.Tests.Domain;

public sealed class NationalIdTests
{
    [Theory]
    [InlineData("10000000146")]
    [InlineData("12345678950")]
    public void Create_accepts_a_valid_tckn(string value)
    {
        var result = NationalId.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1234567895")]      // 10 digits
    [InlineData("123456789501")]    // 12 digits
    [InlineData("1234567895a")]     // not a digit
    [InlineData(" 12345678950")]    // whitespace is not trimmed
    [InlineData("02345678950")]     // starts with 0
    public void Create_rejects_wrong_shape(string? value)
    {
        NationalId.Create(value).Error.Should().Be(CustomerErrors.InvalidNationalId);
    }

    [Theory]
    [InlineData("12345678960")]     // 10th digit wrong
    [InlineData("12345678951")]     // 11th digit wrong
    [InlineData("11111111111")]
    public void Create_rejects_wrong_checksum(string value)
    {
        NationalId.Create(value).Error.Should().Be(CustomerErrors.InvalidNationalId);
    }

    [Fact]
    public void Two_national_ids_with_the_same_value_are_equal()
    {
        NationalId.Create("10000000146").Value.Should().Be(NationalId.Create("10000000146").Value);
    }
}

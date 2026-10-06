using MiniBanking.Customers.Domain;

namespace MiniBanking.Customers.Tests.Domain;

public sealed class PhoneNumberTests
{
    [Fact]
    public void Create_accepts_a_turkish_mobile_number_in_e164_format()
    {
        PhoneNumber.Create("+905321234567").Value.Value.Should().Be("+905321234567");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("05321234567")]       // national format
    [InlineData("+90 532 123 45 67")] // spaces
    [InlineData("+902121234567")]     // landline (not 5xx)
    [InlineData("+90532123456")]      // too short
    [InlineData("+9053212345678")]    // too long
    [InlineData("+15551234567")]      // not Turkey
    public void Create_rejects_anything_else(string? value)
    {
        PhoneNumber.Create(value).Error.Should().Be(CustomerErrors.InvalidPhoneNumber);
    }
}

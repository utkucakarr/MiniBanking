using MiniBanking.Customers.Domain;

namespace MiniBanking.Customers.Tests.Domain;

public sealed class EmailTests
{
    [Fact]
    public void Create_trims_and_lowercases()
    {
        Email.Create("  Ali.Yilmaz@Example.COM ").Value.Value.Should().Be("ali.yilmaz@example.com");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ali")]
    [InlineData("ali@")]
    [InlineData("ali@example")]
    [InlineData("ali@@example.com")]
    [InlineData("ali yilmaz@example.com")]
    public void Create_rejects_invalid_addresses(string? value)
    {
        Email.Create(value).Error.Should().Be(CustomerErrors.InvalidEmail);
    }

    [Fact]
    public void Two_emails_differing_only_in_case_are_equal()
    {
        Email.Create("ALI@example.com").Value.Should().Be(Email.Create("ali@EXAMPLE.com").Value);
    }
}

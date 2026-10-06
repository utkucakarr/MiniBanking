using MiniBanking.Customers.Domain;

namespace MiniBanking.Customers.Tests.Domain;

public sealed class PersonNameTests
{
    [Fact]
    public void Create_trims_both_names()
    {
        var name = PersonName.Create("  Ali Rıza ", " Yılmaz ").Value;

        name.FirstName.Should().Be("Ali Rıza");
        name.LastName.Should().Be("Yılmaz");
    }

    [Theory]
    [InlineData(null, "Yılmaz")]
    [InlineData("Ali", null)]
    [InlineData("   ", "Yılmaz")]
    [InlineData("Ali", "")]
    public void Create_rejects_missing_names(string? firstName, string? lastName)
    {
        PersonName.Create(firstName, lastName).Error.Should().Be(CustomerErrors.InvalidName);
    }

    [Fact]
    public void Create_accepts_names_of_exactly_the_maximum_length()
    {
        var longest = new string('a', PersonName.MaxLength);

        PersonName.Create(longest, longest).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_rejects_too_long_names()
    {
        var tooLong = new string('a', PersonName.MaxLength + 1);

        PersonName.Create(tooLong, "Yılmaz").Error.Should().Be(CustomerErrors.InvalidName);
    }
}

using MiniBanking.Customers.Domain;
using MiniBanking.SharedKernel.Results;

namespace MiniBanking.Customers.Tests.Domain;

public sealed class CustomerTests
{
    // "Today" in these tests: 15 January 2026.
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    private static Result<Customer> Register(DateOnly dateOfBirth, DateTimeOffset? now = null) =>
        Customer.Register(
            PersonName.Create("Ali", "Yılmaz").Value,
            NationalId.Create("10000000146").Value,
            dateOfBirth,
            Email.Create("ali@example.com").Value,
            PhoneNumber.Create("+905321234567").Value,
            now ?? Now);

    [Fact]
    public void Register_creates_a_pending_customer()
    {
        var customer = Register(new DateOnly(1990, 5, 20)).Value;

        customer.Id.Value.Should().NotBeEmpty();
        customer.Name.Should().Be(PersonName.Create("Ali", "Yılmaz").Value);
        customer.NationalId.Should().Be(NationalId.Create("10000000146").Value);
        customer.DateOfBirth.Should().Be(new DateOnly(1990, 5, 20));
        customer.KycStatus.Should().Be(KycStatus.Pending);
        customer.RegisteredAt.Should().Be(Now);
    }

    [Fact]
    public void Register_gives_every_customer_a_new_id()
    {
        Register(new DateOnly(1990, 5, 20)).Value.Id.Should().NotBe(Register(new DateOnly(1990, 5, 20)).Value.Id);
    }

    [Fact]
    public void Register_raises_CustomerRegistered()
    {
        var customer = Register(new DateOnly(1990, 5, 20)).Value;

        customer.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CustomerRegistered>()
            .Which.Should().Match<CustomerRegistered>(e => e.CustomerId == customer.Id && e.OccurredOn == Now);
    }

    [Fact]
    public void Register_accepts_a_customer_whose_18th_birthday_is_today()
    {
        Register(new DateOnly(2008, 1, 15)).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Register_rejects_a_customer_who_turns_18_tomorrow()
    {
        Register(new DateOnly(2008, 1, 16)).Error.Should().Be(CustomerErrors.Underage);
    }

    [Theory]
    [InlineData(2026, 2, 28, false)] // not 18 yet: there is no 29 February in 2026
    [InlineData(2026, 3, 1, true)]
    public void Customer_born_on_29_February_turns_18_on_1_March(int year, int month, int day, bool accepted)
    {
        var now = new DateTimeOffset(year, month, day, 9, 0, 0, TimeSpan.Zero);

        Register(new DateOnly(2008, 2, 29), now).IsSuccess.Should().Be(accepted);
    }

    [Fact]
    public void Register_rejects_a_date_of_birth_in_the_future()
    {
        Register(new DateOnly(2026, 1, 16)).Error.Should().Be(CustomerErrors.DateOfBirthInFuture);
    }
}

using MiniBanking.SharedKernel.Domain;
using MiniBanking.SharedKernel.Results;

namespace MiniBanking.Customers.Domain;

/// <summary>A retail (individual) customer of the bank.</summary>
internal sealed class Customer : AggregateRoot<CustomerId>
{
    public const int MinimumAge = 18;

    private Customer(
        CustomerId id,
        PersonName name,
        NationalId nationalId,
        DateOnly dateOfBirth,
        Email email,
        PhoneNumber phoneNumber,
        DateTimeOffset registeredAt)
        : base(id)
    {
        Name = name;
        NationalId = nationalId;
        DateOfBirth = dateOfBirth;
        Email = email;
        PhoneNumber = phoneNumber;
        KycStatus = KycStatus.Pending;
        RegisteredAt = registeredAt;
    }

    // Used by EF Core when loading a customer from the database.
    private Customer()
    {
    }

    public PersonName Name { get; private set; } = null!;

    public NationalId NationalId { get; private set; } = null!;

    public DateOnly DateOfBirth { get; private set; }

    public Email Email { get; private set; } = null!;

    public PhoneNumber PhoneNumber { get; private set; } = null!;

    public KycStatus KycStatus { get; private set; }

    public DateTimeOffset RegisteredAt { get; private set; }

    /// <summary>The only way to create a customer: checks the rules, then records what happened.</summary>
    /// <param name="now">The current time from <see cref="TimeProvider"/>; the domain never reads the clock itself.</param>
    public static Result<Customer> Register(
        PersonName name,
        NationalId nationalId,
        DateOnly dateOfBirth,
        Email email,
        PhoneNumber phoneNumber,
        DateTimeOffset now)
    {
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        if (dateOfBirth > today)
            return CustomerErrors.DateOfBirthInFuture;

        if (AgeOn(dateOfBirth, today) < MinimumAge)
            return CustomerErrors.Underage;

        var customer = new Customer(CustomerId.New(), name, nationalId, dateOfBirth, email, phoneNumber, now);
        customer.Raise(new CustomerRegistered(customer.Id, now));

        return customer;
    }

    /// <summary>Completed years of age on <paramref name="date"/>.</summary>
    private static int AgeOn(DateOnly dateOfBirth, DateOnly date)
    {
        var age = date.Year - dateOfBirth.Year;

        // The birthday hasn't come yet this year.
        if (dateOfBirth > date.AddYears(-age))
            age--;

        return age;
    }
}

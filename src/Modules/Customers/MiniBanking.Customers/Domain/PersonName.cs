using MiniBanking.SharedKernel.Results;

namespace MiniBanking.Customers.Domain;

/// <summary>A person's legal name. Several given names ("Ali Rıza") all go into <see cref="FirstName"/>.</summary>
internal sealed record PersonName
{
    public const int MaxLength = 100;

    private PersonName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public string FirstName { get; }

    public string LastName { get; }

    public static Result<PersonName> Create(string? firstName, string? lastName)
    {
        var first = firstName?.Trim();
        var last = lastName?.Trim();

        if (string.IsNullOrEmpty(first) || string.IsNullOrEmpty(last)
            || first.Length > MaxLength || last.Length > MaxLength)
            return CustomerErrors.InvalidName;

        return new PersonName(first, last);
    }

    public override string ToString() => $"{FirstName} {LastName}";
}

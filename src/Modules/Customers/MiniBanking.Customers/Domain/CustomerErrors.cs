using MiniBanking.SharedKernel.Results;

namespace MiniBanking.Customers.Domain;

/// <summary>All expected failures of the Customers module, in one place (ADR 0005).</summary>
internal static class CustomerErrors
{
    public static readonly Error InvalidName = Error.Validation(
        "Customers.InvalidName",
        $"First and last name are required and can be at most {PersonName.MaxLength} characters.");

    public static readonly Error InvalidNationalId = Error.Validation(
        "Customers.InvalidNationalId",
        "National id must be a valid 11-digit Turkish identity number (TCKN).");

    public static readonly Error InvalidEmail = Error.Validation(
        "Customers.InvalidEmail",
        "Email address is not valid.");

    public static readonly Error InvalidPhoneNumber = Error.Validation(
        "Customers.InvalidPhoneNumber",
        "Phone number must be a Turkish mobile number in the format +905XXXXXXXXX.");

    public static readonly Error DateOfBirthInFuture = Error.Validation(
        "Customers.DateOfBirthInFuture",
        "Date of birth can't be in the future.");

    public static readonly Error Underage = Error.BusinessRule(
        "Customers.Underage",
        $"Customers must be at least {Customer.MinimumAge} years old.");

    public static readonly Error NationalIdAlreadyRegistered = Error.Conflict(
        "Customers.NationalIdAlreadyRegistered",
        "A customer with this national id is already registered.");

    public static Error NotFound(Guid customerId) => Error.NotFound(
        "Customers.NotFound",
        $"Customer '{customerId}' was not found.");
}

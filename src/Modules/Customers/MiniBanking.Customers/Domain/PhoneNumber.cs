using System.Text.RegularExpressions;
using MiniBanking.SharedKernel.Results;

namespace MiniBanking.Customers.Domain;

/// <summary>
/// A Turkish mobile number in E.164 format: +90, then 5, then 9 more digits (e.g. +905321234567).
/// Clients send the canonical format; converting "0532 123 45 67" is the client's job.
/// </summary>
internal sealed partial record PhoneNumber
{
    public const int Length = 13;

    private PhoneNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<PhoneNumber> Create(string? value)
    {
        if (value is null || !Pattern().IsMatch(value))
            return CustomerErrors.InvalidPhoneNumber;

        return new PhoneNumber(value);
    }

    [GeneratedRegex(@"^\+905\d{9}$")]
    private static partial Regex Pattern();

    public override string ToString() => Value;
}

using System.Text.RegularExpressions;
using MiniBanking.SharedKernel.Results;

namespace MiniBanking.Customers.Domain;

/// <summary>
/// An email address, stored trimmed and in lower case. The check is deliberately simple:
/// the only real proof that an address works is a confirmation email.
/// </summary>
internal sealed partial record Email
{
    public const int MaxLength = 254;

    private Email(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<Email> Create(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(normalized) || normalized.Length > MaxLength || !Pattern().IsMatch(normalized))
            return CustomerErrors.InvalidEmail;

        return new Email(normalized);
    }

    // something@something.something, with no spaces and exactly one @.
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex Pattern();

    public override string ToString() => Value;
}

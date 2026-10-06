using MiniBanking.SharedKernel.Results;

namespace MiniBanking.Customers.Domain;

/// <summary>
/// Turkish identity number (TC Kimlik No, TCKN): 11 digits, the first is not 0,
/// and the last two digits are checksums of the others.
/// </summary>
internal sealed record NationalId
{
    public const int Length = 11;

    private NationalId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<NationalId> Create(string? value)
    {
        if (value is null || value.Length != Length || !value.All(char.IsAsciiDigit) || value[0] == '0')
            return CustomerErrors.InvalidNationalId;

        if (!HasValidChecksum(value))
            return CustomerErrors.InvalidNationalId;

        return new NationalId(value);
    }

    private static bool HasValidChecksum(string value)
    {
        var digits = value.Select(c => c - '0').ToArray();

        // 1st, 3rd, 5th, 7th, 9th digits (indexes 0, 2, 4, 6, 8) and 2nd, 4th, 6th, 8th (indexes 1, 3, 5, 7).
        var oddSum = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
        var evenSum = digits[1] + digits[3] + digits[5] + digits[7];

        // C#'s % can return a negative number, so add 10 before the final % 10.
        var tenthDigit = ((oddSum * 7 - evenSum) % 10 + 10) % 10;
        var eleventhDigit = digits.Take(10).Sum() % 10;

        return digits[9] == tenthDigit && digits[10] == eleventhDigit;
    }

    public override string ToString() => Value;
}

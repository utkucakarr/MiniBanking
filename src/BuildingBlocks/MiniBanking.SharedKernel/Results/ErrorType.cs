namespace MiniBanking.SharedKernel.Results;

/// <summary>
/// Category of an expected failure. The API layer maps each category to an HTTP status code.
/// </summary>
public enum ErrorType
{
    None = 0,
    Validation = 1,
    Unauthorized = 2,
    Forbidden = 3,
    NotFound = 4,
    Conflict = 5,
    BusinessRule = 6
}

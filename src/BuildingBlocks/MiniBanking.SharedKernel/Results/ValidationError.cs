namespace MiniBanking.SharedKernel.Results;

/// <summary>
/// A <see cref="ErrorType.Validation"/> error that carries the failures per field,
/// e.g. { "Amount": ["Amount must be greater than zero."] }.
/// </summary>
public sealed record ValidationError(IReadOnlyDictionary<string, string[]> Errors)
    : Error("Validation.Failed", "One or more validation errors occurred.", ErrorType.Validation);

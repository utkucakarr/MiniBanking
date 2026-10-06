namespace MiniBanking.Customers.Domain;

/// <summary>
/// Strongly typed id: a CustomerId can't be passed where an AccountId is expected (ADR 0004).
/// </summary>
internal readonly record struct CustomerId(Guid Value)
{
    /// <summary>UUID v7: time-ordered, so new rows are appended to the index instead of inserted randomly.</summary>
    public static CustomerId New() => new(Guid.CreateVersion7());
}

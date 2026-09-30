namespace MiniBanking.SharedKernel.Domain;

/// <summary>
/// Something meaningful that happened inside an aggregate, e.g. "AccountFrozen".
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredOn { get; }
}

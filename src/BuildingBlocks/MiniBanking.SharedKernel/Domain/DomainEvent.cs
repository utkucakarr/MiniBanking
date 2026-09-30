namespace MiniBanking.SharedKernel.Domain;

/// <summary>
/// Base record for domain events. The occurrence time is passed in by the caller,
/// who got it from <see cref="TimeProvider"/>, so the domain never reads the system clock.
/// </summary>
public abstract record DomainEvent(DateTimeOffset OccurredOn) : IDomainEvent
{
    // Initialized once at construction: the id must never change after the event is created.
    public Guid EventId { get; } = Guid.CreateVersion7(OccurredOn);
}

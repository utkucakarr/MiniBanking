namespace MiniBanking.SharedKernel.Domain;

/// <summary>
/// The root of a consistency boundary. All changes go through it; it enforces
/// the invariants and records domain events describing what happened.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
    where TId : struct, IEquatable<TId>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    // Used by EF Core when materializing aggregates from the database.
    protected AggregateRoot()
    {
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}

namespace MiniBanking.SharedKernel.Domain;

/// <summary>
/// Non-generic view of an aggregate's domain events, so infrastructure can collect
/// events from any aggregate without knowing its id type.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}

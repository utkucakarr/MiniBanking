using MiniBanking.SharedKernel.Domain;

namespace MiniBanking.Customers.Domain;

/// <summary>Raised when a new customer has been registered.</summary>
internal sealed record CustomerRegistered(CustomerId CustomerId, DateTimeOffset OccurredOn) : DomainEvent(OccurredOn);

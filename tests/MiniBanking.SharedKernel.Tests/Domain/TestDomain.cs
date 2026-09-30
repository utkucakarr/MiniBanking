using MiniBanking.SharedKernel.Domain;

namespace MiniBanking.SharedKernel.Tests.Domain;

// Minimal concrete types used only to test the abstract building blocks.

internal readonly record struct TestId(Guid Value)
{
    public static TestId New() => new(Guid.CreateVersion7());
}

internal sealed record TestHappened(TestId AggregateId, DateTimeOffset OccurredOn) : DomainEvent(OccurredOn);

internal sealed class TestAggregate(TestId id) : AggregateRoot<TestId>(id)
{
    public void DoSomething(DateTimeOffset now) => Raise(new TestHappened(Id, now));
}

internal sealed class OtherTestEntity(TestId id) : Entity<TestId>(id);

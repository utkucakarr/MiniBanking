namespace MiniBanking.SharedKernel.Tests.Domain;

public class AggregateRootTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 29, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Raised_domain_events_are_exposed_and_can_be_cleared()
    {
        var aggregate = new TestAggregate(TestId.New());

        aggregate.DoSomething(Now);

        var domainEvent = aggregate.DomainEvents.Should().ContainSingle().Which;
        domainEvent.Should().BeOfType<TestHappened>();
        domainEvent.OccurredOn.Should().Be(Now);

        aggregate.ClearDomainEvents();

        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Domain_event_id_is_stable()
    {
        var aggregate = new TestAggregate(TestId.New());
        aggregate.DoSomething(Now);

        var domainEvent = aggregate.DomainEvents.Single();

        domainEvent.EventId.Should().Be(domainEvent.EventId);
    }
}

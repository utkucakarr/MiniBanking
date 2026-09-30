namespace MiniBanking.SharedKernel.Tests.Domain;

public class EntityTests
{
    [Fact]
    public void Entities_with_same_id_are_equal()
    {
        var id = TestId.New();

        var first = new TestAggregate(id);
        var second = new TestAggregate(id);

        first.Should().Be(second);
        (first == second).Should().BeTrue();
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void Entities_with_different_ids_are_not_equal()
    {
        var first = new TestAggregate(TestId.New());
        var second = new TestAggregate(TestId.New());

        first.Should().NotBe(second);
        (first != second).Should().BeTrue();
    }

    [Fact]
    public void Entities_of_different_types_with_same_id_are_not_equal()
    {
        var id = TestId.New();

        var aggregate = new TestAggregate(id);
        var other = new OtherTestEntity(id);

        aggregate.Equals(other).Should().BeFalse();
    }
}

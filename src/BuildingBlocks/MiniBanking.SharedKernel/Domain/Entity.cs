namespace MiniBanking.SharedKernel.Domain;

/// <summary>
/// An object defined by its identity rather than its attributes.
/// Two entities are equal when they are of the same type and have the same id.
/// </summary>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : struct, IEquatable<TId>
{
    protected Entity(TId id)
    {
        Id = id;
    }

    // Used by EF Core when materializing entities from the database.
    protected Entity()
    {
    }

    public TId Id { get; private init; }

    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return GetType() == other.GetType() && Id.Equals(other.Id);
    }

    public override bool Equals(object? obj) => obj is Entity<TId> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);
}

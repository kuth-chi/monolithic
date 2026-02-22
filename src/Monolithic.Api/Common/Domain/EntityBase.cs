namespace Monolithic.Api.Common.Domain;

/// <summary>
/// Base class for all persisted domain entities that have a surrogate GUID primary key.
/// Provides identity-by-Id equality — two entities are equal if and only if their Ids match.
///
/// The <see cref="Id"/> setter is public so EF Core model-binding and object
/// initializers (used in Application layer service factories) can assign it.
/// Use <c>Guid.NewGuid()</c> when creating new instances in services.
/// </summary>
public abstract class EntityBase : IEquatable<EntityBase>
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public bool Equals(EntityBase? other)
        => other is not null && Id == other.Id;

    public override bool Equals(object? obj)
        => obj is EntityBase other && Equals(other);

    public override int GetHashCode()
        => Id.GetHashCode();

    public static bool operator ==(EntityBase? left, EntityBase? right)
        => Equals(left, right);

    public static bool operator !=(EntityBase? left, EntityBase? right)
        => !Equals(left, right);
}

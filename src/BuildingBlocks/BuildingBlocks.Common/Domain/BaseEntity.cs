namespace BuildingBlocks.Common.Domain;

/// <summary>
/// Base entity class with Id property.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; }
}

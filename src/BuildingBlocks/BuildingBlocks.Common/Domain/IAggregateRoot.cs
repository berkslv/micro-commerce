namespace BuildingBlocks.Common.Domain;

/// <summary>
/// Marker interface for aggregate roots. Domain events are collected here.
/// </summary>
public interface IAggregateRoot
{
    IReadOnlyList<object> DomainEvents { get; }

    void ClearDomainEvents();
}

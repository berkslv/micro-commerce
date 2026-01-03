namespace BuildingBlocks.Messaging.Models;

/// <summary>
/// Holds CorrelationId for tracking requests across services.
/// </summary>
public class Correlation
{
    public Guid Id { get; init; }
}

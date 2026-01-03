namespace BuildingBlocks.Messaging.Models;

/// <summary>
/// Holds JWT token content for propagation across services.
/// </summary>
public class Token
{
    public string Content { get; init; } = string.Empty;
}

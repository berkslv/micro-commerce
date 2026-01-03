namespace Order.Application.Features.Products.Commands.SyncProductUpdated;

/// <summary>
/// Response for SyncProductUpdatedCommand.
/// </summary>
public sealed record SyncProductUpdatedResponse(
    Guid ProductId,
    bool Success);

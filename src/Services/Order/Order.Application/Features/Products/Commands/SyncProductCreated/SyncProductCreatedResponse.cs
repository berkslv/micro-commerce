namespace Order.Application.Features.Products.Commands.SyncProductCreated;

/// <summary>
/// Response for SyncProductCreatedCommand.
/// </summary>
public sealed record SyncProductCreatedResponse(
    Guid ProductId,
    bool Success);

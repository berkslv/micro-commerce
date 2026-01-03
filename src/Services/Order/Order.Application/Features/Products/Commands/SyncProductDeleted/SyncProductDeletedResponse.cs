namespace Order.Application.Features.Products.Commands.SyncProductDeleted;

/// <summary>
/// Response for SyncProductDeletedCommand.
/// </summary>
public sealed record SyncProductDeletedResponse(
    Guid ProductId,
    bool Success);

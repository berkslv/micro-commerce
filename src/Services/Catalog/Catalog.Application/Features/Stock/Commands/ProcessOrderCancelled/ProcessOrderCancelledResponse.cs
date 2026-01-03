namespace Catalog.Application.Features.Stock.Commands.ProcessOrderCancelled;

/// <summary>
/// Response for ProcessOrderCancelledCommand.
/// </summary>
public sealed record ProcessOrderCancelledResponse(int ReleasedItemCount);

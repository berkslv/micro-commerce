namespace Catalog.Application.Features.Stock.Commands.ProcessOrderCreated;

/// <summary>
/// Response for ProcessOrderCreatedCommand.
/// </summary>
public sealed record ProcessOrderCreatedResponse(
    bool Success,
    string? FailureReason);

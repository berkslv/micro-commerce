using Catalog.Application.Interfaces;
using BuildingBlocks.Common.Exceptions;
using BuildingBlocks.Messaging.Events;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Stock.Commands.ProcessOrderCreated;

/// <summary>
/// Command to process order created event - reserves stock for all items.
/// </summary>
public sealed record ProcessOrderCreatedCommand(
    Guid OrderId,
    string CorrelationId,
    IReadOnlyList<OrderItemData> Items) : IRequest<ProcessOrderCreatedResponse>;

/// <summary>
/// Handler for ProcessOrderCreatedCommand.
/// Handles the full saga logic: reserve all items, rollback on failure, publish events.
/// </summary>
public sealed class ProcessOrderCreatedCommandHandler : IRequestHandler<ProcessOrderCreatedCommand, ProcessOrderCreatedResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;

    public ProcessOrderCreatedCommandHandler(
        IApplicationDbContext context,
        IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<ProcessOrderCreatedResponse> Handle(ProcessOrderCreatedCommand request, CancellationToken cancellationToken)
    {
        var allReservationsSuccessful = true;
        var reservedProducts = new List<(Guid ProductId, int Quantity)>();
        string? failureReason = null;

        foreach (var item in request.Items)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == item.ProductId, cancellationToken);

            if (product is null)
            {
                allReservationsSuccessful = false;
                failureReason = $"Product {item.ProductId} not found";
                break;
            }

            var success = product.ReserveStock(item.Quantity);

            if (success)
            {
                reservedProducts.Add((item.ProductId, item.Quantity));
            }
            else
            {
                allReservationsSuccessful = false;
                failureReason = $"Insufficient stock for product {item.ProductId}";
                break;
            }
        }

        if (allReservationsSuccessful)
        {
            await _context.SaveChangesAsync(cancellationToken);

            await _publishEndpoint.Publish(new StockReservedEvent(
                request.OrderId,
                DateTime.UtcNow,
                request.CorrelationId,
                reservedProducts.Select(p => new ReservedProductData(p.ProductId, p.Quantity)).ToList()),
                cancellationToken);

            return new ProcessOrderCreatedResponse(true, null);
        }
        else
        {
            // Rollback already reserved stock (in-memory, not yet saved)
            foreach (var (productId, quantity) in reservedProducts)
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

                product?.ReleaseStock(quantity);
            }

            // Don't save changes - we're rolling back
            // Clear the change tracker to discard any changes
            _context.ChangeTracker.Clear();

            await _publishEndpoint.Publish(new StockReservationFailedEvent(
                request.OrderId,
                DateTime.UtcNow,
                request.CorrelationId,
                failureReason ?? "Insufficient stock for one or more products"),
                cancellationToken);

            return new ProcessOrderCreatedResponse(false, failureReason);
        }
    }
}

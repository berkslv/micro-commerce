using Order.Application.Interfaces;
using BuildingBlocks.Common.Exceptions;
using BuildingBlocks.Messaging.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Order.Application.Features.Orders.Commands.MarkStockReservationFailed;

/// <summary>
/// Command to mark an order as having failed stock reservation and cancel it.
/// </summary>
public sealed record MarkStockReservationFailedCommand(
    Guid OrderId,
    string Reason) : IRequest<MarkStockReservationFailedResponse>;

/// <summary>
/// Handler for MarkStockReservationFailedCommand.
/// </summary>
public sealed class MarkStockReservationFailedCommandHandler 
    : IRequestHandler<MarkStockReservationFailedCommand, MarkStockReservationFailedResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly Correlation _correlation;

    public MarkStockReservationFailedCommandHandler(IApplicationDbContext context, Correlation correlation)
    {
        _context = context;
        _correlation = correlation;
    }

    public async Task<MarkStockReservationFailedResponse> Handle(
        MarkStockReservationFailedCommand request, 
        CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order is null)
        {
            throw new NotFoundException("Order", request.OrderId);
        }

        // Cancel the order due to stock reservation failure
        // Note: No OrderCancelledEvent will be published since no stock was reserved
        order.Cancel($"Stock reservation failed: {request.Reason}", _correlation.Id.ToString());
        
        await _context.SaveChangesAsync(cancellationToken);

        return new MarkStockReservationFailedResponse(
            order.Id,
            order.Status.ToString(),
            request.Reason);
    }
}

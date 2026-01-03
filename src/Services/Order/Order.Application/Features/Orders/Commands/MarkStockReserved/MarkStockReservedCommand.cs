using Order.Application.Interfaces;
using BuildingBlocks.Common.Exceptions;
using BuildingBlocks.Messaging.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Order.Application.Features.Orders.Commands.MarkStockReserved;

/// <summary>
/// Command to mark an order as having stock reserved and confirm it.
/// </summary>
public sealed record MarkStockReservedCommand(Guid OrderId) : IRequest<MarkStockReservedResponse>;

/// <summary>
/// Handler for MarkStockReservedCommand.
/// </summary>
public sealed class MarkStockReservedCommandHandler : IRequestHandler<MarkStockReservedCommand, MarkStockReservedResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly Correlation _correlation;

    public MarkStockReservedCommandHandler(IApplicationDbContext context, Correlation correlation)
    {
        _context = context;
        _correlation = correlation;
    }

    public async Task<MarkStockReservedResponse> Handle(MarkStockReservedCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order is null)
        {
            throw new NotFoundException("Order", request.OrderId);
        }

        // Mark stock as reserved
        order.MarkStockReserved();
        
        // Automatically confirm the order (publishes OrderConfirmedEvent)
        order.Confirm(_correlation.Id.ToString());
        
        await _context.SaveChangesAsync(cancellationToken);

        return new MarkStockReservedResponse(
            order.Id,
            order.Status.ToString());
    }
}

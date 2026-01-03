using BuildingBlocks.Common.Exceptions;
using BuildingBlocks.Messaging.Models;
using Order.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Order.Application.Features.Orders.Commands.CancelOrder;

/// <summary>
/// Command to cancel an order.
/// </summary>
public sealed record CancelOrderCommand(Guid OrderId, string Reason) : IRequest<CancelOrderResponse>;

/// <summary>
/// Handler for CancelOrderCommand.
/// </summary>
public sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, CancelOrderResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly Correlation _correlation;

    public CancelOrderCommandHandler(IApplicationDbContext context, Correlation correlation)
    {
        _context = context;
        _correlation = correlation;
    }

    public async Task<CancelOrderResponse> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order is null)
        {
            throw new NotFoundException("Order", request.OrderId);
        }

        order.Cancel(request.Reason, _correlation.Id.ToString());

        await _context.SaveChangesAsync(cancellationToken);

        return new CancelOrderResponse(
            order.Id,
            order.Status.ToString(),
            request.Reason);
    }
}

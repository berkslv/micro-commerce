using BuildingBlocks.Common.Exceptions;
using BuildingBlocks.Messaging.Models;
using Order.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Order.Application.Features.Orders.Commands.ConfirmOrder;

/// <summary>
/// Command to confirm an order after stock reservation.
/// </summary>
public sealed record ConfirmOrderCommand(Guid OrderId) : IRequest<ConfirmOrderResponse>;

/// <summary>
/// Handler for ConfirmOrderCommand.
/// </summary>
public sealed class ConfirmOrderCommandHandler : IRequestHandler<ConfirmOrderCommand, ConfirmOrderResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly Correlation _correlation;

    public ConfirmOrderCommandHandler(IApplicationDbContext context, Correlation correlation)
    {
        _context = context;
        _correlation = correlation;
    }

    public async Task<ConfirmOrderResponse> Handle(ConfirmOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order is null)
        {
            throw new NotFoundException("Order", request.OrderId);
        }

        order.Confirm(_correlation.Id.ToString());

        await _context.SaveChangesAsync(cancellationToken);

        return new ConfirmOrderResponse(
            order.Id,
            order.Status.ToString(),
            order.TotalAmount.Amount,
            order.TotalAmount.Currency);
    }
}

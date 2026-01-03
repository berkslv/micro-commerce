using Catalog.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Stock.Commands.ProcessOrderCancelled;

/// <summary>
/// Command to process order cancelled event - releases stock for all items.
/// </summary>
public sealed record ProcessOrderCancelledCommand(
    Guid OrderId,
    IReadOnlyList<CancelledItemData> Items) : IRequest<ProcessOrderCancelledResponse>;

/// <summary>
/// Data for cancelled order item.
/// </summary>
public sealed record CancelledItemData(Guid ProductId, int Quantity);

/// <summary>
/// Handler for ProcessOrderCancelledCommand.
/// Releases stock for all items in the cancelled order.
/// </summary>
public sealed class ProcessOrderCancelledCommandHandler : IRequestHandler<ProcessOrderCancelledCommand, ProcessOrderCancelledResponse>
{
    private readonly IApplicationDbContext _context;

    public ProcessOrderCancelledCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProcessOrderCancelledResponse> Handle(ProcessOrderCancelledCommand request, CancellationToken cancellationToken)
    {
        var releasedCount = 0;

        foreach (var item in request.Items)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == item.ProductId, cancellationToken);

            if (product is not null)
            {
                product.ReleaseStock(item.Quantity);
                releasedCount++;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new ProcessOrderCancelledResponse(releasedCount);
    }
}

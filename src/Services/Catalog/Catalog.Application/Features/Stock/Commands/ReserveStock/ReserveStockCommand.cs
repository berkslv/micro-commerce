using Catalog.Application.Interfaces;
using BuildingBlocks.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Stock.Commands.ReserveStock;

/// <summary>
/// Command to reserve stock for order processing.
/// </summary>
public sealed record ReserveStockCommand(
    Guid ProductId,
    int Quantity,
    Guid OrderId) : IRequest<ReserveStockResponse>;

/// <summary>
/// Handler for ReserveStockCommand.
/// </summary>
public sealed class ReserveStockCommandHandler : IRequestHandler<ReserveStockCommand, ReserveStockResponse>
{
    private readonly IApplicationDbContext _context;

    public ReserveStockCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReserveStockResponse> Handle(ReserveStockCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("Product", request.ProductId);
        }

        var success = product.ReserveStock(request.Quantity);

        await _context.SaveChangesAsync(cancellationToken);

        return new ReserveStockResponse(
            request.ProductId,
            request.OrderId,
            request.Quantity,
            success,
            product.StockQuantity);
    }
}

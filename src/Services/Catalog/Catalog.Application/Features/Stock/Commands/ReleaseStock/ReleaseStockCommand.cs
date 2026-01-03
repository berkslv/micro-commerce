using Catalog.Application.Interfaces;
using BuildingBlocks.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Stock.Commands.ReleaseStock;

/// <summary>
/// Command to release reserved stock (rollback).
/// </summary>
public sealed record ReleaseStockCommand(
    Guid ProductId,
    int Quantity,
    Guid OrderId) : IRequest<ReleaseStockResponse>;

/// <summary>
/// Handler for ReleaseStockCommand.
/// </summary>
public sealed class ReleaseStockCommandHandler : IRequestHandler<ReleaseStockCommand, ReleaseStockResponse>
{
    private readonly IApplicationDbContext _context;

    public ReleaseStockCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReleaseStockResponse> Handle(ReleaseStockCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("Product", request.ProductId);
        }

        product.ReleaseStock(request.Quantity);

        await _context.SaveChangesAsync(cancellationToken);

        return new ReleaseStockResponse(
            request.ProductId,
            request.OrderId,
            request.Quantity,
            product.StockQuantity);
    }
}

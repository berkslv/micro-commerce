using Order.Application.Interfaces;
using Order.Domain.Entities;
using BuildingBlocks.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Order.Application.Features.Products.Commands.SyncProductUpdated;

/// <summary>
/// Command to sync an updated product to the read model.
/// </summary>
public sealed record SyncProductUpdatedCommand(
    Guid ProductId,
    string Name,
    decimal Price,
    string Currency,
    bool IsAvailable) : IRequest<SyncProductUpdatedResponse>;

/// <summary>
/// Handler for SyncProductUpdatedCommand.
/// </summary>
public sealed class SyncProductUpdatedCommandHandler 
    : IRequestHandler<SyncProductUpdatedCommand, SyncProductUpdatedResponse>
{
    private readonly IApplicationDbContext _context;

    public SyncProductUpdatedCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SyncProductUpdatedResponse> Handle(
        SyncProductUpdatedCommand request, 
        CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product is null)
        {
            // Product doesn't exist, create it
            product = Product.Create(
                request.ProductId,
                request.Name,
                request.Price,
                request.Currency,
                request.IsAvailable);

            _context.Products.Add(product);
        }
        else
        {
            product.Update(request.Name, request.Price, request.Currency, request.IsAvailable);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new SyncProductUpdatedResponse(request.ProductId, true);
    }
}

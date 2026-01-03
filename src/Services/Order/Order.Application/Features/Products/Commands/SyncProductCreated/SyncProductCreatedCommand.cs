using Order.Application.Interfaces;
using Order.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Order.Application.Features.Products.Commands.SyncProductCreated;

/// <summary>
/// Command to sync a newly created product to the read model.
/// </summary>
public sealed record SyncProductCreatedCommand(
    Guid ProductId,
    string Name,
    decimal Price,
    string Currency,
    bool IsAvailable) : IRequest<SyncProductCreatedResponse>;

/// <summary>
/// Handler for SyncProductCreatedCommand.
/// </summary>
public sealed class SyncProductCreatedCommandHandler 
    : IRequestHandler<SyncProductCreatedCommand, SyncProductCreatedResponse>
{
    private readonly IApplicationDbContext _context;

    public SyncProductCreatedCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SyncProductCreatedResponse> Handle(
        SyncProductCreatedCommand request, 
        CancellationToken cancellationToken)
    {
        var existingProduct = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (existingProduct is not null)
        {
            // Product already exists, update it instead
            existingProduct.Update(request.Name, request.Price, request.Currency, request.IsAvailable);
        }
        else
        {
            var product = Product.Create(
                request.ProductId,
                request.Name,
                request.Price,
                request.Currency,
                request.IsAvailable);

            _context.Products.Add(product);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new SyncProductCreatedResponse(request.ProductId, true);
    }
}

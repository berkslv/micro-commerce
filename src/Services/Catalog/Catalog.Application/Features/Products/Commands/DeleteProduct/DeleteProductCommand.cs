using Catalog.Application.Interfaces;
using BuildingBlocks.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Products.Commands.DeleteProduct;

/// <summary>
/// Command to delete a product.
/// </summary>
public sealed record DeleteProductCommand(Guid Id) : IRequest<Unit>;

/// <summary>
/// Handler for DeleteProductCommand.
/// </summary>
public sealed class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Unit>
{
    private readonly IApplicationDbContext _context;

    public DeleteProductCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("Product", request.Id);
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

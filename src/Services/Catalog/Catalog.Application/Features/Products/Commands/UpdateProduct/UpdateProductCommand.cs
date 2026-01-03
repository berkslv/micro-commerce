using Catalog.Application.Interfaces;
using Catalog.Domain.ValueObjects;
using BuildingBlocks.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Products.Commands.UpdateProduct;

/// <summary>
/// Command to update an existing product.
/// </summary>
public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    int StockQuantity,
    Guid CategoryId) : IRequest<UpdateProductResponse>;

/// <summary>
/// Handler for UpdateProductCommand.
/// </summary>
public sealed class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, UpdateProductResponse>
{
    private readonly IApplicationDbContext _context;

    public UpdateProductCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UpdateProductResponse> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("Product", request.Id);
        }

        var productName = ProductName.Create(request.Name);
        var price = Money.Create(request.Price, request.Currency);

        product.Update(
            productName,
            request.Description,
            price);

        await _context.SaveChangesAsync(cancellationToken);

        return new UpdateProductResponse(
            product.Id,
            product.Name.Value,
            product.Description,
            product.Price.Amount,
            product.Price.Currency,
            product.Sku.Value,
            product.StockQuantity,
            product.CategoryId,
            product.ModifiedAt);
    }
}

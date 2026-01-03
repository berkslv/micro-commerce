using Catalog.Application.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Domain.ValueObjects;
using MediatR;

namespace Catalog.Application.Features.Products.Commands.CreateProduct;

/// <summary>
/// Command to create a new product.
/// </summary>
public sealed record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    string Currency,
    string SKU,
    int StockQuantity,
    Guid CategoryId) : IRequest<CreateProductResponse>;

/// <summary>
/// Handler for CreateProductCommand.
/// </summary>
public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, CreateProductResponse>
{
    private readonly IApplicationDbContext _context;

    public CreateProductCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CreateProductResponse> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var productName = ProductName.Create(request.Name);
        var price = Money.Create(request.Price, request.Currency);
        var sku = Sku.Create(request.SKU);

        var product = Product.Create(
            productName,
            request.Description,
            price,
            request.StockQuantity,
            sku,
            request.CategoryId);

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateProductResponse(
            product.Id,
            product.Name.Value,
            product.Description,
            product.Price.Amount,
            product.Price.Currency,
            product.Sku.Value,
            product.StockQuantity,
            product.CategoryId,
            product.CreatedAt);
    }
}

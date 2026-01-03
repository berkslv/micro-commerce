using Catalog.Application.Interfaces;
using BuildingBlocks.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Products.Queries.GetProduct;

/// <summary>
/// Query to get a product by ID.
/// </summary>
public sealed record GetProductQuery(Guid Id) : IRequest<GetProductResponse>;

/// <summary>
/// Handler for GetProductQuery.
/// </summary>
public sealed class GetProductQueryHandler : IRequestHandler<GetProductQuery, GetProductResponse>
{
    private readonly IApplicationDbContext _context;

    public GetProductQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GetProductResponse> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("Product", request.Id);
        }

        return new GetProductResponse(
            product.Id,
            product.Name.Value,
            product.Description,
            product.Price.Amount,
            product.Price.Currency,
            product.Sku.Value,
            product.StockQuantity,
            product.CategoryId,
            product.Category?.Name,
            product.CreatedAt,
            product.ModifiedAt);
    }
}

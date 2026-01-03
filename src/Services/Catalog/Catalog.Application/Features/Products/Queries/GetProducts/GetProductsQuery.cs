using Catalog.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Products.Queries.GetProducts;

/// <summary>
/// Query to get all products.
/// </summary>
public sealed record GetProductsQuery : IRequest<IReadOnlyList<GetProductsResponse>>;

/// <summary>
/// Handler for GetProductsQuery.
/// </summary>
public sealed class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, IReadOnlyList<GetProductsResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetProductsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<GetProductsResponse>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .OrderBy(p => p.Name.Value)
            .Select(p => new GetProductsResponse(
                p.Id,
                p.Name.Value,
                p.Description,
                p.Price.Amount,
                p.Price.Currency,
                p.Sku.Value,
                p.StockQuantity,
                p.CategoryId,
                p.Category != null ? p.Category.Name : null))
            .ToListAsync(cancellationToken);

        return products;
    }
}

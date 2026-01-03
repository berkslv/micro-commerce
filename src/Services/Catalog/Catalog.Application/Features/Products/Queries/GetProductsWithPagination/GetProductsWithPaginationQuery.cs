using Catalog.Application.Interfaces;
using Catalog.Application.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Products.Queries.GetProductsWithPagination;

/// <summary>
/// Query to get products with pagination.
/// </summary>
public sealed record GetProductsWithPaginationQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    Guid? CategoryId = null) : IRequest<PaginatedList<GetProductsWithPaginationResponse>>;

/// <summary>
/// Handler for GetProductsWithPaginationQuery.
/// </summary>
public sealed class GetProductsWithPaginationQueryHandler 
    : IRequestHandler<GetProductsWithPaginationQuery, PaginatedList<GetProductsWithPaginationResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetProductsWithPaginationQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<GetProductsWithPaginationResponse>> Handle(
        GetProductsWithPaginationQuery request, 
        CancellationToken cancellationToken)
    {
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .AsQueryable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(p => 
                p.Name.Value.ToLower().Contains(searchTerm) ||
                p.Description.ToLower().Contains(searchTerm) ||
                p.Sku.Value.ToLower().Contains(searchTerm));
        }

        // Apply category filter
        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Name.Value)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new GetProductsWithPaginationResponse(
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

        return new PaginatedList<GetProductsWithPaginationResponse>(
            items, 
            totalCount, 
            request.PageNumber, 
            request.PageSize);
    }
}

using Catalog.Application.Interfaces;
using Catalog.Application.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Categories.Queries.GetCategoriesWithPagination;

/// <summary>
/// Query to get categories with pagination.
/// </summary>
public sealed record GetCategoriesWithPaginationQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null) : IRequest<PaginatedList<GetCategoriesWithPaginationResponse>>;

/// <summary>
/// Handler for GetCategoriesWithPaginationQuery.
/// </summary>
public sealed class GetCategoriesWithPaginationQueryHandler 
    : IRequestHandler<GetCategoriesWithPaginationQuery, PaginatedList<GetCategoriesWithPaginationResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetCategoriesWithPaginationQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<GetCategoriesWithPaginationResponse>> Handle(
        GetCategoriesWithPaginationQuery request, 
        CancellationToken cancellationToken)
    {
        var query = _context.Categories
            .AsNoTracking()
            .AsQueryable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(c => 
                c.Name.ToLower().Contains(searchTerm) ||
                c.Description.ToLower().Contains(searchTerm));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new GetCategoriesWithPaginationResponse(
                c.Id,
                c.Name,
                c.Description,
                c.CreatedAt,
                c.ModifiedAt))
            .ToListAsync(cancellationToken);

        return new PaginatedList<GetCategoriesWithPaginationResponse>(
            items,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }
}

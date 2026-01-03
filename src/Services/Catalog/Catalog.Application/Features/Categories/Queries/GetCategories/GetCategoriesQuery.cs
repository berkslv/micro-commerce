using Catalog.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Categories.Queries.GetCategories;

/// <summary>
/// Query to get all categories.
/// </summary>
public sealed record GetCategoriesQuery : IRequest<IReadOnlyList<GetCategoriesResponse>>;

/// <summary>
/// Handler for GetCategoriesQuery.
/// </summary>
public sealed class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyList<GetCategoriesResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetCategoriesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<GetCategoriesResponse>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new GetCategoriesResponse(
                c.Id,
                c.Name,
                c.Description,
                c.CreatedAt,
                c.ModifiedAt))
            .ToListAsync(cancellationToken);

        return categories;
    }
}

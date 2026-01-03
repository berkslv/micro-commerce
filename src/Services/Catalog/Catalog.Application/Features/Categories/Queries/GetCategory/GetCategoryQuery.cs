using Catalog.Application.Interfaces;
using BuildingBlocks.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Categories.Queries.GetCategory;

/// <summary>
/// Query to get a category by ID.
/// </summary>
public sealed record GetCategoryQuery(Guid Id) : IRequest<GetCategoryResponse>;

/// <summary>
/// Handler for GetCategoryQuery.
/// </summary>
public sealed class GetCategoryQueryHandler : IRequestHandler<GetCategoryQuery, GetCategoryResponse>
{
    private readonly IApplicationDbContext _context;

    public GetCategoryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GetCategoryResponse> Handle(GetCategoryQuery request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category is null)
        {
            throw new NotFoundException("Category", request.Id);
        }

        return new GetCategoryResponse(
            category.Id,
            category.Name,
            category.Description,
            category.CreatedAt,
            category.ModifiedAt);
    }
}

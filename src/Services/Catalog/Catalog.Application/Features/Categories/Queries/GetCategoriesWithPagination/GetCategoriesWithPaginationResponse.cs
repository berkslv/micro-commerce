namespace Catalog.Application.Features.Categories.Queries.GetCategoriesWithPagination;

/// <summary>
/// Response model for GetCategoriesWithPaginationQuery.
/// </summary>
public sealed record GetCategoriesWithPaginationResponse(
    Guid Id,
    string Name,
    string Description,
    DateTime CreatedAt,
    DateTime? ModifiedAt);

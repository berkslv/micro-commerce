namespace Catalog.Application.Features.Categories.Queries.GetCategory;

/// <summary>
/// Response model for GetCategoryQuery.
/// </summary>
public sealed record GetCategoryResponse(
    Guid Id,
    string Name,
    string Description,
    DateTime CreatedAt,
    DateTime? ModifiedAt);

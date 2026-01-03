namespace Catalog.Application.Features.Categories.Queries.GetCategories;

/// <summary>
/// Response model for GetCategoriesQuery.
/// </summary>
public sealed record GetCategoriesResponse(
    Guid Id,
    string Name,
    string Description,
    DateTime CreatedAt,
    DateTime? ModifiedAt);

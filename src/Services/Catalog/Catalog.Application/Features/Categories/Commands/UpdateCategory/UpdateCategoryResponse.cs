namespace Catalog.Application.Features.Categories.Commands.UpdateCategory;

/// <summary>
/// Response model for UpdateCategoryCommand.
/// </summary>
public sealed record UpdateCategoryResponse(
    Guid Id,
    string Name,
    string Description,
    DateTime CreatedAt,
    DateTime? ModifiedAt);

namespace Catalog.Application.Features.Categories.Commands.CreateCategory;

/// <summary>
/// Response model for CreateCategoryCommand.
/// </summary>
public sealed record CreateCategoryResponse(
    Guid Id,
    string Name,
    string Description,
    DateTime CreatedAt);

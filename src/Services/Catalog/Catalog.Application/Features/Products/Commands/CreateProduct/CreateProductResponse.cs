namespace Catalog.Application.Features.Products.Commands.CreateProduct;

/// <summary>
/// Response for CreateProductCommand.
/// </summary>
public sealed record CreateProductResponse(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    string SKU,
    int StockQuantity,
    Guid CategoryId,
    DateTime CreatedAt);

namespace Catalog.Application.Features.Products.Queries.GetProduct;

/// <summary>
/// Response for GetProductQuery.
/// </summary>
public sealed record GetProductResponse(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    string SKU,
    int StockQuantity,
    Guid CategoryId,
    string? CategoryName,
    DateTime CreatedAt,
    DateTime? ModifiedAt);

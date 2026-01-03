namespace Catalog.Application.Features.Products.Queries.GetProducts;

/// <summary>
/// Response for GetProductsQuery.
/// </summary>
public sealed record GetProductsResponse(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    string SKU,
    int StockQuantity,
    Guid CategoryId,
    string? CategoryName);

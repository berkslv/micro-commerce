namespace Catalog.Application.Features.Products.Queries.GetProductsWithPagination;

/// <summary>
/// Response for GetProductsWithPaginationQuery.
/// </summary>
public sealed record GetProductsWithPaginationResponse(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    string SKU,
    int StockQuantity,
    Guid CategoryId,
    string? CategoryName);

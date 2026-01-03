namespace Catalog.Application.Features.Products.Commands.UpdateProduct;

/// <summary>
/// Response for UpdateProductCommand.
/// </summary>
public sealed record UpdateProductResponse(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    string SKU,
    int StockQuantity,
    Guid CategoryId,
    DateTime? ModifiedAt);

using BuildingBlocks.Common.Exceptions;
using Order.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Order.Application.Features.Orders.Queries.GetOrder;

/// <summary>
/// Query to get an order by ID.
/// </summary>
public sealed record GetOrderQuery(Guid Id) : IRequest<GetOrderResponse>;

/// <summary>
/// Handler for GetOrderQuery.
/// </summary>
public sealed class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, GetOrderResponse>
{
    private readonly IApplicationDbContext _context;

    public GetOrderQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GetOrderResponse> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (order is null)
        {
            throw new NotFoundException("Order", request.Id);
        }

        // Get all product IDs from order items
        var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();

        // Fetch products in a single query
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        // Map order items with joined product data
        var items = order.Items.Select(i =>
        {
            products.TryGetValue(i.ProductId, out var product);
            return new OrderItemResponse(
                i.Id,
                i.ProductId,
                product?.Name ?? i.ProductName,  // Use latest product name from sync, fallback to stored name
                i.UnitPrice.Amount,
                i.UnitPrice.Currency,
                i.Quantity,
                i.TotalPrice.Amount,
                product?.Price,                  // Current product price (may differ from order price)
                product?.IsAvailable ?? false);  // Product availability
        }).ToList();

        return new GetOrderResponse(
            order.Id,
            order.CustomerId,
            order.CustomerEmail,
            order.ShippingAddress.FullAddress,
            order.Status.ToString(),
            order.TotalAmount.Amount,
            order.TotalAmount.Currency,
            order.Notes,
            items,
            order.CreatedAt,
            order.ModifiedAt);
    }
}

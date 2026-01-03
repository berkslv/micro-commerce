using Order.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Order.Application.Features.Orders.Queries.GetOrdersByCustomer;

/// <summary>
/// Query to get orders by customer ID.
/// </summary>
public sealed record GetOrdersByCustomerQuery(Guid CustomerId) : IRequest<IReadOnlyList<GetOrdersByCustomerResponse>>;

/// <summary>
/// Handler for GetOrdersByCustomerQuery.
/// </summary>
public sealed class GetOrdersByCustomerQueryHandler 
    : IRequestHandler<GetOrdersByCustomerQuery, IReadOnlyList<GetOrdersByCustomerResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetOrdersByCustomerQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<GetOrdersByCustomerResponse>> Handle(
        GetOrdersByCustomerQuery request, 
        CancellationToken cancellationToken)
    {
        var orders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == request.CustomerId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new GetOrdersByCustomerResponse(
                o.Id,
                o.Status.ToString(),
                o.TotalAmount.Amount,
                o.TotalAmount.Currency,
                o.CreatedAt))
            .ToListAsync(cancellationToken);

        return orders;
    }
}

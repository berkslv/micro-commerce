using BuildingBlocks.Messaging.Models;
using Order.Application.Interfaces;
using Order.Domain.ValueObjects;
using MediatR;
using OrderEntity = Order.Domain.Entities.Order;

namespace Order.Application.Features.Orders.Commands.CreateOrder;

/// <summary>
/// Command to create a new order.
/// </summary>
public sealed record CreateOrderCommand(
    Guid CustomerId,
    string CustomerEmail,
    string Street,
    string City,
    string State,
    string Country,
    string ZipCode,
    string? Notes,
    List<CreateOrderItemDto> Items) : IRequest<CreateOrderResponse>;

/// <summary>
/// DTO for order item in create command.
/// </summary>
public sealed record CreateOrderItemDto(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    string Currency,
    int Quantity);

/// <summary>
/// Handler for CreateOrderCommand.
/// </summary>
public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, CreateOrderResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly Correlation _correlation;

    public CreateOrderCommandHandler(IApplicationDbContext context, Correlation correlation)
    {
        _context = context;
        _correlation = correlation;
    }

    public async Task<CreateOrderResponse> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var shippingAddress = Address.Create(
            request.Street,
            request.City,
            request.State,
            request.Country,
            request.ZipCode);

        var order = OrderEntity.Create(
            request.CustomerId,
            request.CustomerEmail,
            shippingAddress,
            request.Notes);

        foreach (var item in request.Items)
        {
            var unitPrice = Money.Create(item.UnitPrice, item.Currency);
            order.AddItem(item.ProductId, item.ProductName, unitPrice, item.Quantity);
        }

        // Submit the order (triggers domain event for stock reservation)
        order.Submit(_correlation.Id.ToString());

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateOrderResponse(
            order.Id,
            order.CustomerId,
            order.CustomerEmail,
            order.Status.ToString(),
            order.TotalAmount.Amount,
            order.TotalAmount.Currency,
            order.CreatedAt);
    }
}

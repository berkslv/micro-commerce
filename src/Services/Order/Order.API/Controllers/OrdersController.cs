using Order.Application.Features.Orders.Commands.CancelOrder;
using Order.Application.Features.Orders.Commands.ConfirmOrder;
using Order.Application.Features.Orders.Commands.CreateOrder;
using Order.Application.Features.Orders.Queries.GetOrder;
using Order.Application.Features.Orders.Queries.GetOrdersByCustomer;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Order.API.Controllers;

/// <summary>
/// API controller for managing orders.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    public OrdersController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Gets an order by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetOrderResponse>> GetOrder(Guid id)
    {
        var result = await _sender.Send(new GetOrderQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Gets orders by customer ID.
    /// </summary>
    [HttpGet("customer/{customerId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<GetOrdersByCustomerResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GetOrdersByCustomerResponse>>> GetOrdersByCustomer(Guid customerId)
    {
        var result = await _sender.Send(new GetOrdersByCustomerQuery(customerId));
        return Ok(result);
    }

    /// <summary>
    /// Creates a new order.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateOrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateOrderResponse>> CreateOrder([FromBody] CreateOrderCommand command)
    {
        var result = await _sender.Send(command);
        return CreatedAtAction(nameof(GetOrder), new { id = result.Id }, result);
    }

    /// <summary>
    /// Confirms an order.
    /// </summary>
    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType(typeof(ConfirmOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConfirmOrderResponse>> ConfirmOrder(Guid id)
    {
        var result = await _sender.Send(new ConfirmOrderCommand(id));
        return Ok(result);
    }

    /// <summary>
    /// Cancels an order.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(CancelOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CancelOrderResponse>> CancelOrder(Guid id, [FromBody] CancelOrderCommand command)
    {
        if (id != command.OrderId)
        {
            return BadRequest("Order ID in URL does not match command");
        }

        var result = await _sender.Send(command);
        return Ok(result);
    }
}

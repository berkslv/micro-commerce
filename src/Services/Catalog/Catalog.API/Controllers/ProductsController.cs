using Catalog.Application.Features.Products.Commands.CreateProduct;
using Catalog.Application.Features.Products.Commands.DeleteProduct;
using Catalog.Application.Features.Products.Commands.UpdateProduct;
using Catalog.Application.Features.Products.Queries.GetProduct;
using Catalog.Application.Features.Products.Queries.GetProducts;
using Catalog.Application.Features.Products.Queries.GetProductsWithPagination;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.API.Controllers;

/// <summary>
/// API controller for managing products.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Gets all products.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<GetProductsResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GetProductsResponse>>> GetProducts()
    {
        var result = await _sender.Send(new GetProductsQuery());
        return Ok(result);
    }

    /// <summary>
    /// Gets products with pagination.
    /// </summary>
    [HttpGet("paginated")]
    [ProducesResponseType(typeof(Catalog.Application.Models.PaginatedList<GetProductsWithPaginationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Catalog.Application.Models.PaginatedList<GetProductsWithPaginationResponse>>> GetProductsWithPagination(
        [FromQuery] GetProductsWithPaginationQuery query)
    {
        var result = await _sender.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Gets a product by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetProductResponse>> GetProduct(Guid id)
    {
        var result = await _sender.Send(new GetProductQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Creates a new product.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateProductResponse>> CreateProduct([FromBody] CreateProductCommand command)
    {
        var result = await _sender.Send(command);
        return CreatedAtAction(nameof(GetProduct), new { id = result.Id }, result);
    }

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UpdateProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateProductResponse>> UpdateProduct(Guid id, [FromBody] UpdateProductCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest("Route ID does not match command ID.");
        }

        var result = await _sender.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Deletes a product.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteProduct(Guid id)
    {
        await _sender.Send(new DeleteProductCommand(id));
        return NoContent();
    }
}

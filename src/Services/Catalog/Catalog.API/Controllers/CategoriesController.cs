using Catalog.Application.Features.Categories.Commands.CreateCategory;
using Catalog.Application.Features.Categories.Commands.DeleteCategory;
using Catalog.Application.Features.Categories.Commands.UpdateCategory;
using Catalog.Application.Features.Categories.Queries.GetCategories;
using Catalog.Application.Features.Categories.Queries.GetCategoriesWithPagination;
using Catalog.Application.Features.Categories.Queries.GetCategory;
using Catalog.Application.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.API.Controllers;

/// <summary>
/// API controller for managing categories.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ISender _sender;

    public CategoriesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Gets all categories.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<GetCategoriesResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GetCategoriesResponse>>> GetCategories()
    {
        var result = await _sender.Send(new GetCategoriesQuery());
        return Ok(result);
    }

    /// <summary>
    /// Gets categories with pagination.
    /// </summary>
    [HttpGet("paginated")]
    [ProducesResponseType(typeof(PaginatedList<GetCategoriesWithPaginationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<GetCategoriesWithPaginationResponse>>> GetCategoriesWithPagination(
        [FromQuery] GetCategoriesWithPaginationQuery query)
    {
        var result = await _sender.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Gets a category by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetCategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetCategoryResponse>> GetCategory(Guid id)
    {
        var result = await _sender.Send(new GetCategoryQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Creates a new category.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateCategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateCategoryResponse>> CreateCategory([FromBody] CreateCategoryCommand command)
    {
        var result = await _sender.Send(command);
        return CreatedAtAction(nameof(GetCategory), new { id = result.Id }, result);
    }

    /// <summary>
    /// Updates a category.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UpdateCategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateCategoryResponse>> UpdateCategory(Guid id, [FromBody] UpdateCategoryCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest("Category ID in URL does not match command");
        }

        var result = await _sender.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Deletes a category.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteCategory(Guid id)
    {
        await _sender.Send(new DeleteCategoryCommand(id));
        return NoContent();
    }
}

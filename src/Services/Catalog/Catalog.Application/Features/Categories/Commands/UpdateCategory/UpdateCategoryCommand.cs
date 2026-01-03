using Catalog.Application.Interfaces;
using BuildingBlocks.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Categories.Commands.UpdateCategory;

/// <summary>
/// Command to update a category.
/// </summary>
public sealed record UpdateCategoryCommand(
    Guid Id,
    string Name,
    string Description) : IRequest<UpdateCategoryResponse>;

/// <summary>
/// Handler for UpdateCategoryCommand.
/// </summary>
public sealed class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, UpdateCategoryResponse>
{
    private readonly IApplicationDbContext _context;

    public UpdateCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UpdateCategoryResponse> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category is null)
        {
            throw new NotFoundException("Category", request.Id);
        }

        category.Update(request.Name, request.Description);

        await _context.SaveChangesAsync(cancellationToken);

        return new UpdateCategoryResponse(
            category.Id,
            category.Name,
            category.Description,
            category.CreatedAt,
            category.ModifiedAt);
    }
}

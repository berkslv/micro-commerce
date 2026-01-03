using Catalog.Application.Interfaces;
using BuildingBlocks.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Features.Categories.Commands.DeleteCategory;

/// <summary>
/// Command to delete a category.
/// </summary>
public sealed record DeleteCategoryCommand(Guid Id) : IRequest;

/// <summary>
/// Handler for DeleteCategoryCommand.
/// </summary>
public sealed class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category is null)
        {
            throw new NotFoundException("Category", request.Id);
        }

        if (category.Products.Any())
        {
            throw new ValidationException("Category", "Cannot delete category with associated products. Remove products first.");
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

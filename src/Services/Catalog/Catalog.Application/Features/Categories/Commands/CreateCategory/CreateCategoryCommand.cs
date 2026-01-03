using Catalog.Application.Interfaces;
using Catalog.Domain.Entities;
using MediatR;

namespace Catalog.Application.Features.Categories.Commands.CreateCategory;

/// <summary>
/// Command to create a new category.
/// </summary>
public sealed record CreateCategoryCommand(
    string Name,
    string Description) : IRequest<CreateCategoryResponse>;

/// <summary>
/// Handler for CreateCategoryCommand.
/// </summary>
public sealed class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CreateCategoryResponse>
{
    private readonly IApplicationDbContext _context;

    public CreateCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CreateCategoryResponse> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = Category.Create(request.Name, request.Description);

        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateCategoryResponse(
            category.Id,
            category.Name,
            category.Description,
            category.CreatedAt);
    }
}

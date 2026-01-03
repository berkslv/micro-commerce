using Order.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Order.Application.Features.Products.Commands.SyncProductDeleted;

/// <summary>
/// Command to sync a deleted product to the read model.
/// </summary>
public sealed record SyncProductDeletedCommand(Guid ProductId) : IRequest<SyncProductDeletedResponse>;

/// <summary>
/// Handler for SyncProductDeletedCommand.
/// </summary>
public sealed class SyncProductDeletedCommandHandler 
    : IRequestHandler<SyncProductDeletedCommand, SyncProductDeletedResponse>
{
    private readonly IApplicationDbContext _context;

    public SyncProductDeletedCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SyncProductDeletedResponse> Handle(
        SyncProductDeletedCommand request, 
        CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product is not null)
        {
            // Mark as unavailable instead of deleting (for historical orders)
            product.MarkAsUnavailable();
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new SyncProductDeletedResponse(request.ProductId, true);
    }
}

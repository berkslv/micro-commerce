using BuildingBlocks.Common.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Order.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Interceptor for automatically updating audit properties on entities.
/// </summary>
public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries<BaseAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.SetCreatedAt(DateTime.UtcNow);
                // TODO: Set CreatedBy when user context is available
                // entry.Entity.SetCreatedBy(_currentUserService.Name);
            }

            if (entry.State == EntityState.Modified || entry.HasChangedOwnedEntities())
            {
                entry.Entity.SetModifiedAt(DateTime.UtcNow);
                // TODO: Set ModifiedBy when user context is available
                // entry.Entity.SetModifiedBy(_currentUserService.Name);
            }
        }
    }
}

/// <summary>
/// Extension methods for EntityEntry.
/// </summary>
public static class EntityEntryExtensions
{
    public static bool HasChangedOwnedEntities(this EntityEntry entry)
    {
        return entry.References.Any(r =>
            r.TargetEntry?.Metadata.IsOwned() == true &&
            (r.TargetEntry.State == EntityState.Added || r.TargetEntry.State == EntityState.Modified));
    }
}

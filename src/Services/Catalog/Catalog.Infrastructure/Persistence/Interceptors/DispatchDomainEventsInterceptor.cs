using BuildingBlocks.Common.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Catalog.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Interceptor for dispatching domain events after successful save.
/// </summary>
public class DispatchDomainEventsInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        DispatchDomainEvents(eventData.Context).GetAwaiter().GetResult();

        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await DispatchDomainEvents(eventData.Context, cancellationToken);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static async Task DispatchDomainEvents(
        DbContext? context,
        CancellationToken cancellationToken = default)
    {
        if (context is null)
        {
            return;
        }

        var publisher = context.GetService<IPublishEndpoint>();

        var aggregateRoots = context.ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregateRoots
            .SelectMany(ar => ar.DomainEvents)
            .ToList();

        // Clear domain events before publishing
        foreach (var aggregateRoot in aggregateRoots)
        {
            aggregateRoot.ClearDomainEvents();
        }

        // Publish domain events
        foreach (var domainEvent in domainEvents)
        {
            await publisher.Publish(domainEvent, cancellationToken);
        }
    }
}

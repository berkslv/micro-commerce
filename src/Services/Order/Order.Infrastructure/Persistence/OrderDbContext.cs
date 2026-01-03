using System.Reflection;
using BuildingBlocks.Common.Domain;
using Order.Application.Interfaces;
using Order.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderEntity = Order.Domain.Entities.Order;

namespace Order.Infrastructure.Persistence;

/// <summary>
/// Database context for Order service with MassTransit Inbox/Outbox support.
/// </summary>
public sealed class OrderDbContext : DbContext, IApplicationDbContext
{
    private readonly IPublishEndpoint _publishEndpoint;

    public DbSet<OrderEntity> Orders => Set<OrderEntity>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Product> Products => Set<Product>();

    public OrderDbContext(
        DbContextOptions<OrderDbContext> options,
        IPublishEndpoint publishEndpoint) : base(options)
    {
        _publishEndpoint = publishEndpoint;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // MassTransit Inbox/Outbox tables
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities();
        
        // Collect domain events before saving
        var aggregateRoots = ChangeTracker.Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregateRoots
            .SelectMany(ar => ar.DomainEvents)
            .ToList();

        // Clear domain events
        foreach (var aggregateRoot in aggregateRoots)
        {
            aggregateRoot.ClearDomainEvents();
        }

        // Save changes first
        var result = await base.SaveChangesAsync(cancellationToken);

        // Publish domain events after successful save
        foreach (var domainEvent in domainEvents)
        {
            await _publishEndpoint.Publish(domainEvent, domainEvent.GetType(), cancellationToken);
        }

        return result;
    }

    private void UpdateAuditableEntities()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is BuildingBlocks.Common.Domain.BaseAuditableEntity auditableEntity)
            {
                if (entry.State == EntityState.Added)
                {
                    auditableEntity.SetCreatedAt(DateTime.UtcNow);
                }
                else
                {
                    auditableEntity.SetModifiedAt(DateTime.UtcNow);
                }
            }
        }
    }
}

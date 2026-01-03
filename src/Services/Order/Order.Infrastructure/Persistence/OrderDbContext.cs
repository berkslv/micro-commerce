using System.Reflection;
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
    public DbSet<OrderEntity> Orders => Set<OrderEntity>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Product> Products => Set<Product>();

    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
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
}

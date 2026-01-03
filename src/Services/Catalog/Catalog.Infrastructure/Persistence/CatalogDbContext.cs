using System.Reflection;
using Catalog.Application.Interfaces;
using Catalog.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

/// <summary>
/// Database context for Catalog service with MassTransit Inbox/Outbox support.
/// </summary>
public sealed class CatalogDbContext : DbContext, IApplicationDbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
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

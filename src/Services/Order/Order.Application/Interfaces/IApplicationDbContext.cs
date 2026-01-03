using Microsoft.EntityFrameworkCore;
using OrderEntity = Order.Domain.Entities.Order;
using Order.Domain.Entities;

namespace Order.Application.Interfaces;

/// <summary>
/// Application database context interface for Order service.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<OrderEntity> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<Product> Products { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

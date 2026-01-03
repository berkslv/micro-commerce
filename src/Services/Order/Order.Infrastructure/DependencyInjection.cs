using Order.Application.Interfaces;
using Order.Infrastructure.Persistence;
using Order.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Order.Infrastructure;

/// <summary>
/// Extension methods for configuring infrastructure services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddOrderInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register interceptors
        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        // Database
        services.AddDbContext<OrderDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseNpgsql(
                configuration.GetConnectionString("OrderDb"),
                b => b.MigrationsAssembly(typeof(OrderDbContext).Assembly.FullName));
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<OrderDbContext>());

        services.AddScoped<OrderDbContextInitialiser>();

        return services;
    }
}

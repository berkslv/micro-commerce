using Catalog.Application.Interfaces;
using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Infrastructure;

/// <summary>
/// Extension methods for configuring infrastructure services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register interceptors
        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        // Database
        services.AddDbContext<CatalogDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseNpgsql(
                configuration.GetConnectionString("CatalogDb"),
                b => b.MigrationsAssembly(typeof(CatalogDbContext).Assembly.FullName));
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<CatalogDbContext>());

        services.AddScoped<CatalogDbContextInitialiser>();

        return services;
    }
}

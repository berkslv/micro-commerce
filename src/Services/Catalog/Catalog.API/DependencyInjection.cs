using BuildingBlocks.Messaging.Filters.Correlations;
using BuildingBlocks.Messaging.Filters.Localization;
using BuildingBlocks.Messaging.Filters.Tokens;
using BuildingBlocks.Messaging.Models;
using Catalog.API.Consumers;
using Catalog.Infrastructure.Persistence;
using MassTransit;
using MassTransit.Logging;

namespace Catalog.API;

/// <summary>
/// Extension methods for configuring API layer services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Catalog API services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCatalogApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add controllers
        services.AddControllers();
        
        // Add OpenAPI support (replaces Swashbuckle in .NET 9+)
        services.AddOpenApi();

        // Add HttpContextAccessor for correlation and token propagation
        services.AddHttpContextAccessor();

        // Register correlation and token services
        services.AddScoped<Correlation>();
        services.AddScoped<Token>();
        
        // Register middleware as scoped services (for IMiddleware pattern)
        services.AddScoped<CorrelationMiddleware>();
        services.AddScoped<TokenMiddleware>();
        services.AddScoped<LocalizationMiddleware>();

        // Add MassTransit with RabbitMQ
        services.AddMassTransit(cfg =>
        {
            cfg.SetKebabCaseEndpointNameFormatter();

            // Register consumers
            cfg.AddConsumer<OrderCreatedConsumer>();
            cfg.AddConsumer<OrderCancelledConsumer>();

            cfg.UsingRabbitMq((context, configurator) =>
            {
                configurator.Host(configuration.GetConnectionString("RabbitMq"));

                // Configure consume filters
                configurator.UseConsumeFilter(typeof(CorrelationConsumeFilter<>), context);
                configurator.UseConsumeFilter(typeof(TokenConsumeFilter<>), context);
                configurator.UseConsumeFilter(typeof(LocalizationConsumeFilter<>), context);

                // Configure publish filters
                configurator.UsePublishFilter(typeof(CorrelationPublishFilter<>), context);
                configurator.UsePublishFilter(typeof(TokenPublishFilter<>), context);
                configurator.UsePublishFilter(typeof(LocalizationPublishFilter<>), context);

                // Configure send filters
                configurator.UseSendFilter(typeof(CorrelationSendFilter<>), context);
                configurator.UseSendFilter(typeof(TokenSendFilter<>), context);
                configurator.UseSendFilter(typeof(LocalizationSendFilter<>), context);

                configurator.ConfigureEndpoints(context);
            });

            // Entity Framework Outbox for reliable messaging
            cfg.AddEntityFrameworkOutbox<CatalogDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });
        });

        return services;
    }
}

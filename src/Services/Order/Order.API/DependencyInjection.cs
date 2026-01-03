using BuildingBlocks.Messaging.Filters.Correlations;
using BuildingBlocks.Messaging.Filters.Localization;
using BuildingBlocks.Messaging.Filters.Tokens;
using BuildingBlocks.Messaging.Models;
using Order.API.Consumers;
using Order.Infrastructure.Persistence;
using MassTransit;
using Serilog;

namespace Order.API;

/// <summary>
/// Extension methods for configuring API layer services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Order API services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOrderApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

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
            cfg.AddConsumer<StockReservedConsumer>();
            cfg.AddConsumer<StockReservationFailedConsumer>();
            cfg.AddConsumer<ProductCreatedConsumer>();
            cfg.AddConsumer<ProductUpdatedConsumer>();
            cfg.AddConsumer<ProductDeletedConsumer>();

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
            cfg.AddEntityFrameworkOutbox<OrderDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });
        });

        return services;
    }
}

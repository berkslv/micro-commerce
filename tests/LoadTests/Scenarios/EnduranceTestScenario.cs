using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using System.Text;
using System.Text.Json;

namespace LoadTests.Scenarios;

/// <summary>
/// Endurance test scenario: sustained 100 RPS for 30 minutes.
/// Tests system stability over extended periods - finds memory leaks, resource exhaustion.
/// </summary>
public static class EnduranceTestScenario
{
    public const string ScenarioName = "endurance_test";
    public const int TargetRps = 100;
    public static readonly TimeSpan Duration = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan WarmupDuration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Creates a basic endurance test - sustained load for 30 minutes.
    /// </summary>
    public static ScenarioProps Create(LoadTestConfig config)
    {
        var catalogClient = new HttpClient
        {
            BaseAddress = new Uri(config.BaseUrls.CatalogApi)
        };

        var orderClient = new HttpClient
        {
            BaseAddress = new Uri(config.BaseUrls.OrderApi)
        };

        return Scenario.Create(ScenarioName, async context =>
        {
            // Read products
            var getProducts = await Step.Run("get_products", context, async () =>
            {
                var request = Http.CreateRequest("GET", "/api/products")
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(catalogClient, request);
                return response;
            });

            // Read categories
            var getCategories = await Step.Run("get_categories", context, async () =>
            {
                var request = Http.CreateRequest("GET", "/api/categories")
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(catalogClient, request);
                return response;
            });

            // Read orders
            var getOrders = await Step.Run("get_customer_orders", context, async () =>
            {
                var request = Http.CreateRequest("GET", $"/api/orders/customer/{config.TestData.CustomerId}")
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(orderClient, request);
                return response;
            });

            return Response.Ok();
        })
        .WithoutWarmUp()
        .WithWarmUpDuration(WarmupDuration)
        .WithLoadSimulations(
            Simulation.Inject(
                rate: TargetRps,
                interval: TimeSpan.FromSeconds(1),
                during: Duration
            )
        );
    }

    /// <summary>
    /// Creates a shorter endurance test (10 minutes) for CI/CD pipelines.
    /// </summary>
    public static ScenarioProps CreateShort(LoadTestConfig config)
    {
        var catalogClient = new HttpClient
        {
            BaseAddress = new Uri(config.BaseUrls.CatalogApi)
        };

        return Scenario.Create($"{ScenarioName}_short", async context =>
        {
            var getProducts = await Step.Run("get_products", context, async () =>
            {
                var request = Http.CreateRequest("GET", "/api/products")
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(catalogClient, request);
                return response;
            });

            var getCategories = await Step.Run("get_categories", context, async () =>
            {
                var request = Http.CreateRequest("GET", "/api/categories")
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(catalogClient, request);
                return response;
            });

            return Response.Ok();
        })
        .WithoutWarmUp()
        .WithWarmUpDuration(TimeSpan.FromSeconds(10))
        .WithLoadSimulations(
            Simulation.Inject(
                rate: TargetRps,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromMinutes(10)
            )
        );
    }

    /// <summary>
    /// Creates an endurance test with mixed read/write operations.
    /// </summary>
    public static ScenarioProps CreateWithWrites(LoadTestConfig config)
    {
        var catalogClient = new HttpClient
        {
            BaseAddress = new Uri(config.BaseUrls.CatalogApi)
        };

        var orderClient = new HttpClient
        {
            BaseAddress = new Uri(config.BaseUrls.OrderApi)
        };

        return Scenario.Create($"{ScenarioName}_with_writes", async context =>
        {
            // Read products
            var getProducts = await Step.Run("get_products", context, async () =>
            {
                var request = Http.CreateRequest("GET", "/api/products")
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(catalogClient, request);
                return response;
            });

            // Create order every 20th iteration (5% write rate)
            if (context.InvocationNumber % 20 == 0)
            {
                var createOrder = await Step.Run("create_order", context, async () =>
                {
                    var orderData = new
                    {
                        customerId = config.TestData.CustomerId,
                        items = new[]
                        {
                            new { productId = config.TestData.ProductId, quantity = 1, price = 49.99m }
                        }
                    };

                    var json = JsonSerializer.Serialize(orderData);
                    var request = Http.CreateRequest("POST", "/api/orders")
                        .WithHeader("Content-Type", "application/json")
                        .WithBody(new StringContent(json, Encoding.UTF8, "application/json"));

                    var response = await Http.Send(orderClient, request);
                    return response;
                });
            }

            return Response.Ok();
        })
        .WithoutWarmUp()
        .WithWarmUpDuration(WarmupDuration)
        .WithLoadSimulations(
            Simulation.Inject(
                rate: TargetRps,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromMinutes(15) // 15 minutes for write-heavy endurance
            )
        );
    }

    /// <summary>
    /// Creates an endurance test with concurrent virtual users.
    /// </summary>
    public static ScenarioProps CreateConcurrentUsers(LoadTestConfig config)
    {
        var catalogClient = new HttpClient
        {
            BaseAddress = new Uri(config.BaseUrls.CatalogApi)
        };

        var orderClient = new HttpClient
        {
            BaseAddress = new Uri(config.BaseUrls.OrderApi)
        };

        return Scenario.Create($"{ScenarioName}_concurrent", async context =>
        {
            // Simulate user journey
            var getProducts = await Step.Run("browse_products", context, async () =>
            {
                var request = Http.CreateRequest("GET", "/api/products")
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(catalogClient, request);
                return response;
            });

            // Simulate user think time
            await Task.Delay(Random.Shared.Next(500, 2000));

            var getProductDetails = await Step.Run("get_product_details", context, async () =>
            {
                var request = Http.CreateRequest("GET", $"/api/products/{config.TestData.ProductId}")
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(catalogClient, request);
                return response;
            });

            // Simulate user think time
            await Task.Delay(Random.Shared.Next(1000, 3000));

            var getCategories = await Step.Run("browse_categories", context, async () =>
            {
                var request = Http.CreateRequest("GET", "/api/categories")
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(catalogClient, request);
                return response;
            });

            return Response.Ok();
        })
        .WithoutWarmUp()
        .WithWarmUpDuration(WarmupDuration)
        .WithLoadSimulations(
            // Maintain 50 concurrent users for 20 minutes
            Simulation.KeepConstant(
                copies: 50,
                during: TimeSpan.FromMinutes(20)
            )
        );
    }
}

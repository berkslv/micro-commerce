using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using System.Text;
using System.Text.Json;

namespace LoadTests.Scenarios;

/// <summary>
/// Stress test scenario: incrementing load to find breaking point.
/// Starts at 50 RPS and increases until system degrades.
/// </summary>
public static class StressTestScenario
{
    public const string ScenarioName = "stress_test";
    public const int StartRps = 50;
    public const int MaxRps = 500;
    public const int StepIncrement = 50;
    public static readonly TimeSpan StepDuration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Creates a stress test that incrementally increases load.
    /// Pattern: 50 → 100 → 150 → 200 → 250 → 300 → 350 → 400 → 450 → 500 RPS
    /// </summary>
    public static ScenarioProps Create(LoadTestConfig config)
    {
        var catalogClient = new HttpClient
        {
            BaseAddress = new Uri(config.BaseUrls.CatalogApi)
        };

        return Scenario.Create(ScenarioName, async context =>
        {
            // Primary endpoint for stress testing
            var getProducts = await Step.Run("get_products", context, async () =>
            {
                var request = Http.CreateRequest("GET", "/api/products")
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(catalogClient, request);
                return response;
            });

            return Response.Ok();
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            // Incrementing load pattern
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: StepDuration),
            Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: StepDuration),
            Simulation.Inject(rate: 150, interval: TimeSpan.FromSeconds(1), during: StepDuration),
            Simulation.Inject(rate: 200, interval: TimeSpan.FromSeconds(1), during: StepDuration),
            Simulation.Inject(rate: 250, interval: TimeSpan.FromSeconds(1), during: StepDuration),
            Simulation.Inject(rate: 300, interval: TimeSpan.FromSeconds(1), during: StepDuration),
            Simulation.Inject(rate: 350, interval: TimeSpan.FromSeconds(1), during: StepDuration),
            Simulation.Inject(rate: 400, interval: TimeSpan.FromSeconds(1), during: StepDuration),
            Simulation.Inject(rate: 450, interval: TimeSpan.FromSeconds(1), during: StepDuration),
            Simulation.Inject(rate: 500, interval: TimeSpan.FromSeconds(1), during: StepDuration)
        );
    }

    /// <summary>
    /// Creates a stress test that includes write operations.
    /// Tests how the system handles order creation under increasing load.
    /// </summary>
    public static ScenarioProps CreateWithWriteOperations(LoadTestConfig config)
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
            // Step 1: Get products (read)
            var getProducts = await Step.Run("get_products", context, async () =>
            {
                var request = Http.CreateRequest("GET", "/api/products")
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(catalogClient, request);
                return response;
            });

            // Step 2: Create order (write) - only every 10th iteration to avoid overwhelming DB
            if (context.InvocationNumber % 10 == 0)
            {
                var createOrder = await Step.Run("create_order", context, async () =>
                {
                    var orderData = new
                    {
                        customerId = config.TestData.CustomerId,
                        items = new[]
                        {
                            new { productId = config.TestData.ProductId, quantity = 1, price = 99.99m }
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
        .WithLoadSimulations(
            // Incrementing load with smaller steps for write-heavy scenario
            Simulation.Inject(rate: 25, interval: TimeSpan.FromSeconds(1), during: StepDuration),
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: StepDuration),
            Simulation.Inject(rate: 75, interval: TimeSpan.FromSeconds(1), during: StepDuration),
            Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: StepDuration),
            Simulation.Inject(rate: 125, interval: TimeSpan.FromSeconds(1), during: StepDuration),
            Simulation.Inject(rate: 150, interval: TimeSpan.FromSeconds(1), during: StepDuration)
        );
    }

    /// <summary>
    /// Creates a concurrent users stress test.
    /// Simulates increasing number of concurrent users making requests.
    /// </summary>
    public static ScenarioProps CreateConcurrentUsers(LoadTestConfig config)
    {
        var catalogClient = new HttpClient
        {
            BaseAddress = new Uri(config.BaseUrls.CatalogApi)
        };

        return Scenario.Create($"{ScenarioName}_concurrent", async context =>
        {
            var getProducts = await Step.Run("get_products", context, async () =>
            {
                var request = Http.CreateRequest("GET", "/api/products")
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(catalogClient, request);
                return response;
            });

            // Simulate think time between requests
            await Task.Delay(Random.Shared.Next(100, 500));

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
        .WithLoadSimulations(
            // Increasing concurrent users
            Simulation.KeepConstant(copies: 10, during: TimeSpan.FromSeconds(30)),
            Simulation.KeepConstant(copies: 25, during: TimeSpan.FromSeconds(30)),
            Simulation.KeepConstant(copies: 50, during: TimeSpan.FromSeconds(30)),
            Simulation.KeepConstant(copies: 100, during: TimeSpan.FromSeconds(30)),
            Simulation.KeepConstant(copies: 150, during: TimeSpan.FromSeconds(30)),
            Simulation.KeepConstant(copies: 200, during: TimeSpan.FromSeconds(30))
        );
    }
}

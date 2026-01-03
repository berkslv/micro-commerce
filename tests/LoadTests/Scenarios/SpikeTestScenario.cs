using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace LoadTests.Scenarios;

/// <summary>
/// Spike test scenario: sudden 500 RPS spike.
/// Tests system behavior when traffic suddenly increases dramatically.
/// </summary>
public static class SpikeTestScenario
{
    public const string ScenarioName = "spike_test";
    public const int BaselineRps = 50;
    public const int SpikeRps = 500;
    public static readonly TimeSpan BaselineDuration = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan SpikeDuration = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan RecoveryDuration = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Creates a spike test scenario.
    /// Pattern: Baseline (50 RPS) → Spike (500 RPS) → Recovery (50 RPS)
    /// </summary>
    public static ScenarioProps Create(LoadTestConfig config)
    {
        var catalogClient = new HttpClient
        {
            BaseAddress = new Uri(config.BaseUrls.CatalogApi)
        };

        return Scenario.Create(ScenarioName, async context =>
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
        .WithLoadSimulations(
            // Phase 1: Baseline load
            Simulation.Inject(
                rate: BaselineRps,
                interval: TimeSpan.FromSeconds(1),
                during: BaselineDuration
            ),
            // Phase 2: Sudden spike to 500 RPS
            Simulation.Inject(
                rate: SpikeRps,
                interval: TimeSpan.FromSeconds(1),
                during: SpikeDuration
            ),
            // Phase 3: Return to baseline - observe recovery
            Simulation.Inject(
                rate: BaselineRps,
                interval: TimeSpan.FromSeconds(1),
                during: RecoveryDuration
            )
        );
    }

    /// <summary>
    /// Creates a double spike test.
    /// Tests system ability to handle multiple sudden traffic surges.
    /// </summary>
    public static ScenarioProps CreateDoubleSpike(LoadTestConfig config)
    {
        var catalogClient = new HttpClient
        {
            BaseAddress = new Uri(config.BaseUrls.CatalogApi)
        };

        var orderClient = new HttpClient
        {
            BaseAddress = new Uri(config.BaseUrls.OrderApi)
        };

        return Scenario.Create($"{ScenarioName}_double", async context =>
        {
            // Read products
            var getProducts = await Step.Run("get_products", context, async () =>
            {
                var request = Http.CreateRequest("GET", "/api/products")
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
        .WithLoadSimulations(
            // Baseline
            Simulation.Inject(rate: BaselineRps, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            // First spike
            Simulation.Inject(rate: SpikeRps, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20)),
            // Recovery period
            Simulation.Inject(rate: BaselineRps, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            // Second spike
            Simulation.Inject(rate: SpikeRps, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20)),
            // Final recovery
            Simulation.Inject(rate: BaselineRps, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
        );
    }

    /// <summary>
    /// Creates a gradual spike test with ramp up and ramp down.
    /// </summary>
    public static ScenarioProps CreateGradualSpike(LoadTestConfig config)
    {
        var catalogClient = new HttpClient
        {
            BaseAddress = new Uri(config.BaseUrls.CatalogApi)
        };

        return Scenario.Create($"{ScenarioName}_gradual", async context =>
        {
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
            // Baseline
            Simulation.Inject(rate: BaselineRps, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            // Ramp up to spike
            Simulation.RampingInject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10)),
            Simulation.RampingInject(rate: 200, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10)),
            Simulation.RampingInject(rate: 350, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10)),
            Simulation.RampingInject(rate: SpikeRps, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10)),
            // Hold at peak
            Simulation.Inject(rate: SpikeRps, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20)),
            // Ramp down
            Simulation.RampingInject(rate: 350, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10)),
            Simulation.RampingInject(rate: 200, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10)),
            Simulation.RampingInject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10)),
            Simulation.RampingInject(rate: BaselineRps, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10)),
            // Recovery observation
            Simulation.Inject(rate: BaselineRps, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
        );
    }
}

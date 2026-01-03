using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace LoadTests.Scenarios;

/// <summary>
/// Peak load scenario: 200 RPS (2x normal) for 3 minutes.
/// Simulates high-traffic periods like sales events or promotions.
/// </summary>
public static class PeakLoadScenario
{
    public const string ScenarioName = "peak_load";
    public const int TargetRps = 200; // 2x normal load
    public static readonly TimeSpan Duration = TimeSpan.FromMinutes(3);
    public static readonly TimeSpan WarmupDuration = TimeSpan.FromSeconds(15);

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
            // Get products - main endpoint for peak load testing
            var getProducts = await Step.Run("get_products", context, async () =>
            {
                var request = Http.CreateRequest("GET", "/api/products")
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(catalogClient, request);
                return response;
            });

            // Get categories - secondary read operation
            var getCategories = await Step.Run("get_categories", context, async () =>
            {
                var request = Http.CreateRequest("GET", "/api/categories")
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(catalogClient, request);
                return response;
            });

            // Get orders - check order service under load
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
    /// Creates a peak load scenario with ramping up pattern.
    /// </summary>
    public static ScenarioProps CreateWithRampUp(LoadTestConfig config)
    {
        var catalogClient = new HttpClient
        {
            BaseAddress = new Uri(config.BaseUrls.CatalogApi)
        };

        return Scenario.Create($"{ScenarioName}_ramp_up", async context =>
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
            // Ramp up from 50 to 200 RPS over 1 minute
            Simulation.RampingInject(
                rate: 50,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(30)
            ),
            Simulation.RampingInject(
                rate: TargetRps,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(30)
            ),
            // Hold at peak for 2 minutes
            Simulation.Inject(
                rate: TargetRps,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromMinutes(2)
            ),
            // Ramp down
            Simulation.RampingInject(
                rate: 50,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(30)
            )
        );
    }
}

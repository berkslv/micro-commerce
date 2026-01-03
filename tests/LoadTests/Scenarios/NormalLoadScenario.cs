using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using System.Text;
using System.Text.Json;

namespace LoadTests.Scenarios;

/// <summary>
/// Normal load scenario: 100 RPS baseline for 2 minutes.
/// Represents typical daily traffic patterns.
/// </summary>
public static class NormalLoadScenario
{
    public const string ScenarioName = "normal_load";
    public const int TargetRps = 100;
    public static readonly TimeSpan Duration = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan WarmupDuration = TimeSpan.FromSeconds(10);

    public static ScenarioProps Create(LoadTestConfig config)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(config.BaseUrls.CatalogApi)
        };

        return Scenario.Create(ScenarioName, async context =>
        {
            var step1 = await Step.Run("get_products", context, async () =>
            {
                var request = Http.CreateRequest("GET", "/api/products")
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(httpClient, request);
                return response;
            });

            var step2 = await Step.Run("get_categories", context, async () =>
            {
                var request = Http.CreateRequest("GET", "/api/categories")
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(httpClient, request);
                return response;
            });

            var step3 = await Step.Run("health_check", context, async () =>
            {
                var request = Http.CreateRequest("GET", "/health")
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(httpClient, request);
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

    public static ScenarioProps CreateWithOrders(LoadTestConfig config)
    {
        var catalogClient = new HttpClient
        {
            BaseAddress = new Uri(config.BaseUrls.CatalogApi)
        };

        var orderClient = new HttpClient
        {
            BaseAddress = new Uri(config.BaseUrls.OrderApi)
        };

        return Scenario.Create($"{ScenarioName}_with_orders", async context =>
        {
            // Read operation - GET products
            var getProducts = await Step.Run("get_products", context, async () =>
            {
                var request = Http.CreateRequest("GET", "/api/products")
                    .WithHeader("Accept", "application/json");

                var response = await Http.Send(catalogClient, request);
                return response;
            });

            // Read operation - GET orders by customer
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
}

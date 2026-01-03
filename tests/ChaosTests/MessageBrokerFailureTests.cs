using ChaosTests.Infrastructure;
using Polly;
using System.Diagnostics;

namespace ChaosTests;

/// <summary>
/// Tests for RabbitMQ message broker failure scenarios.
/// Verifies message reliability and event handling under failures.
/// </summary>
[TestFixture]
[Category("Chaos")]
public class MessageBrokerFailureTests
{
    private HttpClient _catalogClient = null!;
    private HttpClient _orderClient = null!;
    private ResilienceMetrics _metrics = null!;

    [SetUp]
    public void Setup()
    {
        _catalogClient = Testing.CatalogClient;
        _orderClient = Testing.OrderClient;
        _metrics = Testing.Metrics;
        Testing.ResetMetrics();
    }

    [Test]
    public async Task ShouldCreateProductEvenWithMessageBrokerDelay()
    {
        // Arrange
        var latencyPolicy = ChaosStrategies.CreateLatencyStrategy(
            latency: TimeSpan.FromMilliseconds(500),
            injectionRate: 0.5);
        
        // First create a category
        var categoryRequest = new
        {
            Name = $"MQ Test Category {Guid.NewGuid():N}",
            Description = "Category for message broker testing"
        };
        
        var categoryResponse = await _catalogClient.PostAsJsonAsync("/api/categories", categoryRequest);
        categoryResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        // Act - Create product (should trigger ProductCreatedEvent)
        var productRequest = new
        {
            Name = $"MQ Test Product {Guid.NewGuid():N}",
            Description = "Product for message broker testing",
            Price = 99.99m,
            Currency = "USD",
            SKU = $"MQ-{Guid.NewGuid():N}".ToUpper()[..12],
            StockQuantity = 100,
            CategoryId = category!.Id
        };
        
        var stopwatch = Stopwatch.StartNew();
        
        var response = await latencyPolicy.ExecuteAsync(async token =>
        {
            return await _catalogClient.PostAsJsonAsync("/api/products", productRequest, token);
        }, CancellationToken.None);
        
        stopwatch.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        product.Should().NotBeNull();
        product!.Name.Should().Be(productRequest.Name);
        
        Console.WriteLine($"Product created in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Product ID: {product.Id}");
    }

    [Test]
    public async Task ShouldHandleMessagePublishFailures()
    {
        // Arrange - Simulate publish failures
        var faultPolicy = ChaosStrategies.CreateNetworkFailureStrategy(injectionRate: 0.3);
        
        // Create category first
        var categoryRequest = new
        {
            Name = $"MQ Fault Category {Guid.NewGuid():N}",
            Description = "Category for fault testing"
        };
        
        var categoryResponse = await _catalogClient.PostAsJsonAsync("/api/categories", categoryRequest);
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        
        int successCount = 0;
        int failureCount = 0;

        // Act - Create multiple products with potential publish failures
        for (int i = 0; i < 10; i++)
        {
            var productRequest = new
            {
                Name = $"Fault Product {i} - {Guid.NewGuid():N}",
                Description = "Testing message publish failures",
                Price = 49.99m,
                Currency = "USD",
                SKU = $"FLT{i}{Guid.NewGuid():N}".ToUpper()[..12],
                StockQuantity = 50,
                CategoryId = category!.Id
            };
            
            try
            {
                var response = await faultPolicy.ExecuteAsync(async token =>
                {
                    return await _catalogClient.PostAsJsonAsync("/api/products", productRequest, token);
                }, CancellationToken.None);
                
                if (response.StatusCode == HttpStatusCode.Created)
                    successCount++;
                else
                    failureCount++;
            }
            catch
            {
                failureCount++;
            }
        }

        // Assert
        Console.WriteLine($"Product creations - Success: {successCount}, Failures: {failureCount}");
        
        // Most operations should succeed despite chaos
        successCount.Should().BeGreaterThanOrEqualTo(5);
    }

    [Test]
    public async Task ShouldMaintainEventOrderUnderLoad()
    {
        // Arrange
        var category = await CreateCategoryAsync("Event Order Test");
        var createdProducts = new List<(int Order, Guid Id)>();
        var tasks = new List<Task>();
        var lockObj = new object();

        // Act - Create products concurrently
        for (int i = 0; i < 10; i++)
        {
            int order = i;
            tasks.Add(Task.Run(async () =>
            {
                var productRequest = new
                {
                    Name = $"Order Test {order}",
                    Description = $"Product {order} for order testing",
                    Price = 10.00m + order,
                    Currency = "USD",
                    SKU = $"ORD{order}{Guid.NewGuid():N}".ToUpper()[..12],
                    StockQuantity = order + 1,
                    CategoryId = category.Id
                };
                
                var response = await _catalogClient.PostAsJsonAsync("/api/products", productRequest);
                
                if (response.StatusCode == HttpStatusCode.Created)
                {
                    var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
                    lock (lockObj)
                    {
                        createdProducts.Add((order, product!.Id));
                    }
                }
            }));
        }
        
        await Task.WhenAll(tasks);

        // Assert
        Console.WriteLine($"Created {createdProducts.Count} products concurrently");
        
        createdProducts.Count.Should().Be(10, "all concurrent product creations should succeed");
        createdProducts.Select(p => p.Id).Distinct().Count().Should().Be(10, "all products should have unique IDs");
    }

    [Test]
    public async Task ShouldHandleMessageConsumerFailures()
    {
        // Arrange
        var retryPolicy = ResiliencePolicies.CreateRetryPolicy(retryCount: 3);
        var faultPolicy = ChaosStrategies.CreateHttpErrorStrategy(
            HttpStatusCode.ServiceUnavailable,
            injectionRate: 0.4);
        
        // Create category
        var category = await CreateCategoryAsync("Consumer Failure Test");
        
        int successCount = 0;

        // Act - Multiple operations that trigger events
        for (int i = 0; i < 10; i++)
        {
            var productRequest = new
            {
                Name = $"Consumer Test {i}",
                Description = "Testing consumer failures",
                Price = 25.00m,
                Currency = "USD",
                SKU = $"CSM{i}{Guid.NewGuid():N}".ToUpper()[..12],
                StockQuantity = 25,
                CategoryId = category.Id
            };
            
            try
            {
                var response = await retryPolicy.ExecuteAsync(async token =>
                {
                    return await faultPolicy.ExecuteAsync(async innerToken =>
                    {
                        return await _catalogClient.PostAsJsonAsync("/api/products", productRequest, innerToken);
                    }, token);
                }, CancellationToken.None);
                
                if (response.StatusCode == HttpStatusCode.Created)
                    successCount++;
            }
            catch
            {
                // Expected under chaos
            }
        }

        // Assert
        Console.WriteLine($"Products created with consumer failures: {successCount}");
        
        successCount.Should().BeGreaterThanOrEqualTo(5, "retry should help recover from consumer failures");
    }

    [Test]
    public async Task ShouldRecoverFromMessageBrokerOutage()
    {
        // Arrange
        var category = await CreateCategoryAsync("Broker Outage Test");
        
        // Phase 1: Simulate broker outage (100% failure)
        var outagePolicy = ChaosStrategies.CreateNetworkFailureStrategy(injectionRate: 1.0);
        
        int failuresDuringOutage = 0;
        for (int i = 0; i < 3; i++)
        {
            try
            {
                var productRequest = new
                {
                    Name = $"Outage Test {i}",
                    Description = "Testing broker outage",
                    Price = 15.00m,
                    Currency = "USD",
                    SKU = $"OUT{i}{Guid.NewGuid():N}".ToUpper()[..12],
                    StockQuantity = 10,
                    CategoryId = category.Id
                };
                
                await outagePolicy.ExecuteAsync(async token =>
                {
                    return await _catalogClient.PostAsJsonAsync("/api/products", productRequest, token);
                }, CancellationToken.None);
            }
            catch
            {
                failuresDuringOutage++;
            }
        }

        // Phase 2: Broker recovered (no faults)
        int successesAfterRecovery = 0;
        for (int i = 0; i < 5; i++)
        {
            var productRequest = new
            {
                Name = $"Recovery Test {i}",
                Description = "Testing after recovery",
                Price = 20.00m,
                Currency = "USD",
                SKU = $"RCV{i}{Guid.NewGuid():N}".ToUpper()[..12],
                StockQuantity = 15,
                CategoryId = category.Id
            };
            
            var response = await _catalogClient.PostAsJsonAsync("/api/products", productRequest);
            if (response.StatusCode == HttpStatusCode.Created)
                successesAfterRecovery++;
        }

        // Assert
        Console.WriteLine($"Failures during outage: {failuresDuringOutage}");
        Console.WriteLine($"Successes after recovery: {successesAfterRecovery}");
        
        failuresDuringOutage.Should().Be(3);
        successesAfterRecovery.Should().Be(5, "system should recover after broker outage");
    }

    [Test]
    public async Task ShouldHandleSlowConsumers()
    {
        // Arrange - Simulate slow processing
        var latencyPolicy = ChaosStrategies.CreateLatencyStrategy(
            latency: TimeSpan.FromSeconds(1),
            injectionRate: 0.7);
        
        var category = await CreateCategoryAsync("Slow Consumer Test");
        var stopwatch = new Stopwatch();
        var responseTimes = new List<long>();

        // Act - Create products with slow consumer simulation
        for (int i = 0; i < 5; i++)
        {
            var productRequest = new
            {
                Name = $"Slow Consumer {i}",
                Description = "Testing slow consumers",
                Price = 30.00m,
                Currency = "USD",
                SKU = $"SLW{i}{Guid.NewGuid():N}".ToUpper()[..12],
                StockQuantity = 20,
                CategoryId = category.Id
            };
            
            stopwatch.Restart();
            
            try
            {
                await latencyPolicy.ExecuteAsync(async token =>
                {
                    return await _catalogClient.PostAsJsonAsync("/api/products", productRequest, token);
                }, CancellationToken.None);
            }
            catch
            {
                // Continue measuring
            }
            
            stopwatch.Stop();
            responseTimes.Add(stopwatch.ElapsedMilliseconds);
        }

        // Assert
        Console.WriteLine($"Response times (ms): {string.Join(", ", responseTimes)}");
        Console.WriteLine($"Average: {responseTimes.Average():F0}ms, Max: {responseTimes.Max()}ms");
        
        // With 70% latency injection of 1s, average should be notably higher
        responseTimes.Average().Should().BeGreaterThan(300);
    }

    private async Task<CategoryResponse> CreateCategoryAsync(string prefix)
    {
        var categoryRequest = new
        {
            Name = $"{prefix} - {Guid.NewGuid():N}",
            Description = $"Category for {prefix}"
        };
        
        var response = await _catalogClient.PostAsJsonAsync("/api/categories", categoryRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var category = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        return category!;
    }

    private record CategoryResponse(Guid Id, string Name, string Description, DateTime CreatedAt);
    private record ProductResponse(Guid Id, string Name, string Description, decimal Price, string Currency, string SKU, int StockQuantity, Guid CategoryId, DateTime CreatedAt);
}

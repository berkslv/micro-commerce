using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using System.Text;
using System.Text.Json;

namespace LoadTests.Scenarios;

/// <summary>
/// Write operations scenario: Tests CREATE, UPDATE, DELETE operations.
/// Focuses on command performance under load.
/// </summary>
public static class WriteOperationsScenario
{
    public const string ScenarioName = "write_operations";
    public const int TargetRps = 50; // Lower RPS for write operations
    public static readonly TimeSpan Duration = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Creates a scenario that tests product CRUD operations.
    /// </summary>
    public static ScenarioProps CreateProductCrud(LoadTestConfig config)
    {
        var catalogClient = new HttpClient
        {
            BaseAddress = new Uri(config.BaseUrls.CatalogApi)
        };

        return Scenario.Create($"{ScenarioName}_product_crud", async context =>
        {
            var productId = Guid.NewGuid();
            
            // Step 1: Create Product
            var createProduct = await Step.Run("create_product", context, async () =>
            {
                var productData = new
                {
                    id = productId,
                    name = $"Load Test Product {context.InvocationNumber}",
                    description = "Product created during load testing",
                    price = 99.99m,
                    categoryId = config.TestData.CategoryId,
                    stock = 100
                };

                var json = JsonSerializer.Serialize(productData);
                var request = Http.CreateRequest("POST", "/api/products")
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(new StringContent(json, Encoding.UTF8, "application/json"));

                var response = await Http.Send(catalogClient, request);
                return response;
            });

            // Step 2: Update Product
            var updateProduct = await Step.Run("update_product", context, async () =>
            {
                var updateData = new
                {
                    id = productId,
                    name = $"Updated Load Test Product {context.InvocationNumber}",
                    description = "Product updated during load testing",
                    price = 149.99m,
                    categoryId = config.TestData.CategoryId,
                    stock = 50
                };

                var json = JsonSerializer.Serialize(updateData);
                var request = Http.CreateRequest("PUT", $"/api/products/{productId}")
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(new StringContent(json, Encoding.UTF8, "application/json"));

                var response = await Http.Send(catalogClient, request);
                return response;
            });

            // Step 3: Delete Product
            var deleteProduct = await Step.Run("delete_product", context, async () =>
            {
                var request = Http.CreateRequest("DELETE", $"/api/products/{productId}");
                var response = await Http.Send(catalogClient, request);
                return response;
            });

            return Response.Ok();
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.Inject(
                rate: TargetRps,
                interval: TimeSpan.FromSeconds(1),
                during: Duration
            )
        );
    }

    /// <summary>
    /// Creates a scenario that tests category CRUD operations.
    /// </summary>
    public static ScenarioProps CreateCategoryCrud(LoadTestConfig config)
    {
        var catalogClient = new HttpClient
        {
            BaseAddress = new Uri(config.BaseUrls.CatalogApi)
        };

        return Scenario.Create($"{ScenarioName}_category_crud", async context =>
        {
            var categoryId = Guid.NewGuid();

            // Step 1: Create Category
            var createCategory = await Step.Run("create_category", context, async () =>
            {
                var categoryData = new
                {
                    id = categoryId,
                    name = $"Load Test Category {context.InvocationNumber}",
                    description = "Category created during load testing"
                };

                var json = JsonSerializer.Serialize(categoryData);
                var request = Http.CreateRequest("POST", "/api/categories")
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(new StringContent(json, Encoding.UTF8, "application/json"));

                var response = await Http.Send(catalogClient, request);
                return response;
            });

            // Step 2: Update Category
            var updateCategory = await Step.Run("update_category", context, async () =>
            {
                var updateData = new
                {
                    id = categoryId,
                    name = $"Updated Load Test Category {context.InvocationNumber}",
                    description = "Category updated during load testing"
                };

                var json = JsonSerializer.Serialize(updateData);
                var request = Http.CreateRequest("PUT", $"/api/categories/{categoryId}")
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(new StringContent(json, Encoding.UTF8, "application/json"));

                var response = await Http.Send(catalogClient, request);
                return response;
            });

            // Step 3: Delete Category
            var deleteCategory = await Step.Run("delete_category", context, async () =>
            {
                var request = Http.CreateRequest("DELETE", $"/api/categories/{categoryId}");
                var response = await Http.Send(catalogClient, request);
                return response;
            });

            return Response.Ok();
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.Inject(
                rate: 25, // Lower rate for categories
                interval: TimeSpan.FromSeconds(1),
                during: Duration
            )
        );
    }

    /// <summary>
    /// Creates a scenario that tests order CRUD operations.
    /// </summary>
    public static ScenarioProps CreateOrderCrud(LoadTestConfig config)
    {
        var orderClient = new HttpClient
        {
            BaseAddress = new Uri(config.BaseUrls.OrderApi)
        };

        return Scenario.Create($"{ScenarioName}_order_crud", async context =>
        {
            var orderId = Guid.NewGuid();

            // Step 1: Create Order
            var createOrder = await Step.Run("create_order", context, async () =>
            {
                var orderData = new
                {
                    id = orderId,
                    customerId = config.TestData.CustomerId,
                    items = new[]
                    {
                        new 
                        { 
                            productId = config.TestData.ProductId, 
                            productName = "Test Product",
                            quantity = Random.Shared.Next(1, 5), 
                            price = 99.99m 
                        }
                    },
                    shippingAddress = new
                    {
                        street = "123 Test Street",
                        city = "Test City",
                        state = "TS",
                        country = "Test Country",
                        zipCode = "12345"
                    }
                };

                var json = JsonSerializer.Serialize(orderData);
                var request = Http.CreateRequest("POST", "/api/orders")
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(new StringContent(json, Encoding.UTF8, "application/json"));

                var response = await Http.Send(orderClient, request);
                return response;
            });

            // Step 2: Update Order Status
            var updateOrder = await Step.Run("update_order_status", context, async () =>
            {
                var updateData = new
                {
                    orderId = orderId,
                    status = "Processing"
                };

                var json = JsonSerializer.Serialize(updateData);
                var request = Http.CreateRequest("PUT", $"/api/orders/{orderId}/status")
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(new StringContent(json, Encoding.UTF8, "application/json"));

                var response = await Http.Send(orderClient, request);
                return response;
            });

            // Step 3: Cancel Order (soft delete)
            var cancelOrder = await Step.Run("cancel_order", context, async () =>
            {
                var request = Http.CreateRequest("DELETE", $"/api/orders/{orderId}");
                var response = await Http.Send(orderClient, request);
                return response;
            });

            return Response.Ok();
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.Inject(
                rate: TargetRps,
                interval: TimeSpan.FromSeconds(1),
                during: Duration
            )
        );
    }

    /// <summary>
    /// Creates a mixed scenario with both read and write operations.
    /// Ratio: 70% reads, 30% writes (realistic production ratio).
    /// </summary>
    public static ScenarioProps CreateMixedReadWrite(LoadTestConfig config)
    {
        var catalogClient = new HttpClient
        {
            BaseAddress = new Uri(config.BaseUrls.CatalogApi)
        };

        var orderClient = new HttpClient
        {
            BaseAddress = new Uri(config.BaseUrls.OrderApi)
        };

        return Scenario.Create($"{ScenarioName}_mixed", async context =>
        {
            var operationType = Random.Shared.Next(100);

            if (operationType < 40) // 40% - Read products
            {
                var getProducts = await Step.Run("get_products", context, async () =>
                {
                    var request = Http.CreateRequest("GET", "/api/products")
                        .WithHeader("Accept", "application/json");
                    var response = await Http.Send(catalogClient, request);
                    return response;
                });
            }
            else if (operationType < 60) // 20% - Read categories
            {
                var getCategories = await Step.Run("get_categories", context, async () =>
                {
                    var request = Http.CreateRequest("GET", "/api/categories")
                        .WithHeader("Accept", "application/json");
                    var response = await Http.Send(catalogClient, request);
                    return response;
                });
            }
            else if (operationType < 70) // 10% - Read orders
            {
                var getOrders = await Step.Run("get_orders", context, async () =>
                {
                    var request = Http.CreateRequest("GET", $"/api/orders/customer/{config.TestData.CustomerId}")
                        .WithHeader("Accept", "application/json");
                    var response = await Http.Send(orderClient, request);
                    return response;
                });
            }
            else if (operationType < 85) // 15% - Create order
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
            else if (operationType < 95) // 10% - Create product
            {
                var createProduct = await Step.Run("create_product", context, async () =>
                {
                    var productData = new
                    {
                        name = $"Load Test Product {Guid.NewGuid():N}",
                        description = "Product created during load testing",
                        price = Random.Shared.Next(10, 500),
                        categoryId = config.TestData.CategoryId,
                        stock = Random.Shared.Next(10, 100)
                    };

                    var json = JsonSerializer.Serialize(productData);
                    var request = Http.CreateRequest("POST", "/api/products")
                        .WithHeader("Content-Type", "application/json")
                        .WithBody(new StringContent(json, Encoding.UTF8, "application/json"));

                    var response = await Http.Send(catalogClient, request);
                    return response;
                });
            }
            else // 5% - Create category
            {
                var createCategory = await Step.Run("create_category", context, async () =>
                {
                    var categoryData = new
                    {
                        name = $"Load Test Category {Guid.NewGuid():N}",
                        description = "Category created during load testing"
                    };

                    var json = JsonSerializer.Serialize(categoryData);
                    var request = Http.CreateRequest("POST", "/api/categories")
                        .WithHeader("Content-Type", "application/json")
                        .WithBody(new StringContent(json, Encoding.UTF8, "application/json"));

                    var response = await Http.Send(catalogClient, request);
                    return response;
                });
            }

            return Response.Ok();
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.Inject(
                rate: 100,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromMinutes(3)
            )
        );
    }

    /// <summary>
    /// Creates a write-heavy scenario (80% writes, 20% reads).
    /// Useful for testing database write performance.
    /// </summary>
    public static ScenarioProps CreateWriteHeavy(LoadTestConfig config)
    {
        var catalogClient = new HttpClient
        {
            BaseAddress = new Uri(config.BaseUrls.CatalogApi)
        };

        var orderClient = new HttpClient
        {
            BaseAddress = new Uri(config.BaseUrls.OrderApi)
        };

        return Scenario.Create($"{ScenarioName}_write_heavy", async context =>
        {
            var operationType = Random.Shared.Next(100);

            if (operationType < 10) // 10% - Read products
            {
                var getProducts = await Step.Run("get_products", context, async () =>
                {
                    var request = Http.CreateRequest("GET", "/api/products")
                        .WithHeader("Accept", "application/json");
                    var response = await Http.Send(catalogClient, request);
                    return response;
                });
            }
            else if (operationType < 20) // 10% - Read orders
            {
                var getOrders = await Step.Run("get_orders", context, async () =>
                {
                    var request = Http.CreateRequest("GET", $"/api/orders/customer/{config.TestData.CustomerId}")
                        .WithHeader("Accept", "application/json");
                    var response = await Http.Send(orderClient, request);
                    return response;
                });
            }
            else if (operationType < 50) // 30% - Create orders
            {
                var createOrder = await Step.Run("create_order", context, async () =>
                {
                    var orderData = new
                    {
                        customerId = config.TestData.CustomerId,
                        items = new[]
                        {
                            new { productId = config.TestData.ProductId, quantity = Random.Shared.Next(1, 5), price = 99.99m }
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
            else if (operationType < 80) // 30% - Create products
            {
                var createProduct = await Step.Run("create_product", context, async () =>
                {
                    var productData = new
                    {
                        name = $"Load Test Product {Guid.NewGuid():N}",
                        description = "Product created during load testing",
                        price = Random.Shared.Next(10, 500),
                        categoryId = config.TestData.CategoryId,
                        stock = Random.Shared.Next(10, 100)
                    };

                    var json = JsonSerializer.Serialize(productData);
                    var request = Http.CreateRequest("POST", "/api/products")
                        .WithHeader("Content-Type", "application/json")
                        .WithBody(new StringContent(json, Encoding.UTF8, "application/json"));

                    var response = await Http.Send(catalogClient, request);
                    return response;
                });
            }
            else // 20% - Create categories
            {
                var createCategory = await Step.Run("create_category", context, async () =>
                {
                    var categoryData = new
                    {
                        name = $"Load Test Category {Guid.NewGuid():N}",
                        description = "Category created during load testing"
                    };

                    var json = JsonSerializer.Serialize(categoryData);
                    var request = Http.CreateRequest("POST", "/api/categories")
                        .WithHeader("Content-Type", "application/json")
                        .WithBody(new StringContent(json, Encoding.UTF8, "application/json"));

                    var response = await Http.Send(catalogClient, request);
                    return response;
                });
            }

            return Response.Ok();
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.Inject(
                rate: 75, // Slightly lower RPS for write-heavy
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromMinutes(3)
            )
        );
    }

    /// <summary>
    /// Creates a burst write scenario - simulates flash sale or promotion.
    /// </summary>
    public static ScenarioProps CreateBurstWrites(LoadTestConfig config)
    {
        var orderClient = new HttpClient
        {
            BaseAddress = new Uri(config.BaseUrls.OrderApi)
        };

        return Scenario.Create($"{ScenarioName}_burst_writes", async context =>
        {
            // Simulate flash sale - many orders at once
            var createOrder = await Step.Run("create_order_burst", context, async () =>
            {
                var orderData = new
                {
                    customerId = Guid.NewGuid().ToString(), // Different customers
                    items = new[]
                    {
                        new 
                        { 
                            productId = config.TestData.ProductId, 
                            quantity = Random.Shared.Next(1, 3), 
                            price = 49.99m 
                        }
                    }
                };

                var json = JsonSerializer.Serialize(orderData);
                var request = Http.CreateRequest("POST", "/api/orders")
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(new StringContent(json, Encoding.UTF8, "application/json"));

                var response = await Http.Send(orderClient, request);
                return response;
            });

            return Response.Ok();
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            // Normal period
            Simulation.Inject(rate: 20, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            // Burst period (flash sale start)
            Simulation.Inject(rate: 200, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            // Sustained high load
            Simulation.Inject(rate: 150, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
            // Gradual decline
            Simulation.Inject(rate: 75, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            // Back to normal
            Simulation.Inject(rate: 20, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
        );
    }
}

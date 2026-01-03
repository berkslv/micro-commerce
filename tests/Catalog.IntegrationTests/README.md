# Catalog Integration Tests

This project contains integration tests for the Catalog service API endpoints using real infrastructure.

## Prerequisites

- **Docker**: The tests use [Testcontainers](https://testcontainers.com/) to spin up real PostgreSQL and RabbitMQ containers. Docker must be running on your machine.

## Technologies Used

- **xUnit**: Test framework
- **FluentAssertions**: Assertion library
- **Microsoft.AspNetCore.Mvc.Testing**: WebApplicationFactory for in-process API testing
- **Testcontainers**: PostgreSQL 16 and RabbitMQ 3 containers
- **MassTransit**: Test harness for event verification

## Test Structure

```
Catalog.IntegrationTests/
├── Infrastructure/
│   └── CatalogApiFactory.cs       # Custom WebApplicationFactory with Testcontainers
├── Api/
│   ├── ProductsControllerTests.cs  # Product CRUD endpoint tests
│   └── CategoriesControllerTests.cs # Category CRUD endpoint tests
├── Messaging/
│   └── ProductEventsTests.cs       # Domain event publishing tests
└── GlobalUsings.cs
```

## Running Tests

### From Command Line

```bash
# Run all integration tests
dotnet test tests/Catalog.IntegrationTests/Catalog.IntegrationTests.csproj

# Run with detailed output
dotnet test tests/Catalog.IntegrationTests/Catalog.IntegrationTests.csproj --logger "console;verbosity=detailed"

# Run specific test class
dotnet test tests/Catalog.IntegrationTests/Catalog.IntegrationTests.csproj --filter "FullyQualifiedName~ProductsControllerTests"
```

### From Visual Studio / VS Code

Simply run the tests from the Test Explorer.

## Test Coverage

### Products API (ProductsControllerTests)
- `POST /api/products` - Create product
- `GET /api/products/{id}` - Get single product
- `GET /api/products` - Get all products
- `PUT /api/products/{id}` - Update product
- `DELETE /api/products/{id}` - Delete product
- Validation error scenarios
- Database persistence verification

### Categories API (CategoriesControllerTests)
- `POST /api/categories` - Create category
- `GET /api/categories/{id}` - Get single category
- `GET /api/categories` - Get all categories
- `PUT /api/categories/{id}` - Update category
- `DELETE /api/categories/{id}` - Delete category
- Validation error scenarios
- Database persistence verification

### Event Publishing (ProductEventsTests)
- `ProductCreatedEvent` published when product is created
- `ProductUpdatedEvent` published when product is updated
- Event data verification

## Notes

1. **Container Startup Time**: First test run may take longer as Docker images are pulled.
2. **Isolated Tests**: Each test class gets its own container instances via `IAsyncLifetime`.
3. **Database Reset**: Use `ResetDatabaseAsync()` between tests to ensure test isolation.
4. **Test Harness**: MassTransit test harness captures events in-memory for verification.

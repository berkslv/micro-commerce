# Complete Microservice Project Plan - Without Custom Agents

---

## 📋 Table of Contents
1. [Project Overview](#project-overview)
2. [Architecture Summary](#architecture-summary)
3. [Technology Stack](#technology-stack)
4. [Solution Structure](#solution-structure)
5. [Database Design](#database-design)
6. [Event Catalog](#event-catalog)
7. [API Endpoints](#api-endpoints)
8. [Choreography Saga Flow](#choreography-saga-flow)
9. [Complete Task Breakdown](#complete-task-breakdown)
10. [Testing Strategy](#testing-strategy)
11. [CI/CD Pipeline](#cicd-pipeline)
12. [Docker Configuration](#docker-configuration)
13. [Getting Started Guide](#getting-started-guide)

---

## 🎯 Project Overview

### **Project Name:** E-Commerce Microservices Platform

### **Description:**
A production-ready microservices system demonstrating modern . NET 10 development with Clean Architecture, Event-Driven Choreography, CQRS, and comprehensive testing including chaos engineering. 

### **Timeline:** 4 Weeks (1 Month)

### **Key Features:**
- ✅ Clean Architecture with DDD (no repository pattern)
- ✅ Direct DbContext access in MediatR handlers via IApplicationDbContext
- ✅ Event-Driven Choreography (no orchestrator)
- ✅ MassTransit built-in Inbox/Outbox pattern with shared filters
- ✅ Event Sourcing for data synchronization
- ✅ Hard stock reservation with compensation
- ✅ Exception-based error handling (no Result pattern)
- ✅ Command/Query with separate validators and responses
- ✅ 80% test coverage (unit, integration, chaos, load)
- ✅ Database per service pattern
- ✅ API Gateway with YARP
- ✅ Keycloak authentication
- ✅ Automated CI/CD

---

## 🏗️ Architecture Summary

### **System Components**

```
┌─────────────────────────────────────────────────────────────────┐
│                    API Gateway (YARP) - Port 5000               │
│         Authentication | Routing | Rate Limiting                │
└────────────────────────┬────────────────────────────────────────┘
                         │
        ┌────────────────┴────────────────┐
        ▼                                 ▼
┌──────────────┐                   ┌──────────────┐
│   Catalog    │                   │    Order     │
│   Service    │                   │   Service    │
│  Port 5001   │                   │  Port 5002   │
└──────┬───────┘                   └──────┬───────┘
       │                                  │
       ▼                                  ▼
┌──────────┐                       ┌─────────────┐
│PostgreSQL│                       │ PostgreSQL  │
│catalog_db│                       │  order_db   │
└──────────┘                       │┌───────────┐│
                                   ││order_     ││
                                   ││schema     ││
                                   │├───────────┤│
                                   ││product_   ││
                                   ││read_schema││
                                   │└───────────┘│
                                   └─────────────┘

        └────────────────┬────────────────┘
                         │
                         ▼
        ┌────────────────────────────────┐
        │   RabbitMQ - Port 5672/15672   │
        │   Event Bus (Choreography)     │
        │   MassTransit Inbox/Outbox     │
        └────────────────────────────────┘
                         │
                         ▼
        ┌────────────────────────────────┐
        │   Keycloak - Port 8080         │
        │   Identity & Access Management │
        └────────────────────────────────┘
```

### **Communication Patterns**
- **External → Services:** REST via API Gateway
- **Service → Service:** Event-Driven (RabbitMQ)
- **Real-time validation:** Events (not gRPC)
- **Data sync:** Event Sourcing to Read Models

---

## 🛠️ Technology Stack

### **Core Technologies**

| Category             | Technology | Version | Purpose                      |
| -------------------- | ---------- | ------- | ---------------------------- |
| **Runtime**          | .NET       | 10.0    | Application framework        |
| **Language**         | C#         | 12.0    | Programming language         |
| **API Gateway**      | YARP       | Latest  | Reverse proxy & routing      |
| **Identity**         | Keycloak   | 23.0    | Authentication/Authorization |
| **Message Broker**   | RabbitMQ   | 3.12    | Event bus                    |
| **Database**         | PostgreSQL | 16.0    | Data persistence             |
| **Containerization** | Docker     | Latest  | Container platform           |

### **Libraries & Frameworks**

| Category              | Library               | Purpose                  |
| --------------------- | --------------------- | ------------------------ |
| **CQRS**              | MediatR               | Command/Query separation |
| **Messaging**         | MassTransit           | RabbitMQ abstraction     |
| **ORM**               | Entity Framework Core | Database access          |
| **Validation**        | FluentValidation      | Input validation         |
| **Mapping**           | AutoMapper            | Object mapping           |
| **Resilience**        | Polly                 | Retry, circuit breaker   |
| **Logging**           | Serilog               | Structured logging       |
| **Testing**           | xUnit                 | Unit testing framework   |
| **Mocking**           | NSubstitute           | Test doubles             |
| **Assertions**        | FluentAssertions      | Readable assertions      |
| **Integration Tests** | Testcontainers        | Docker-based testing     |
| **Chaos Testing**     | Simmy                 | Fault injection          |
| **Load Testing**      | NBomber               | Performance testing      |
| **Coverage**          | Coverlet              | Code coverage            |

---

## 📁 Solution Structure

```
Micro/
├── src/
│   ├── ApiGateway/
│   │   ├── ApiGateway.csproj
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── yarp.json
│   │
│   ├── Services/
│   │   ├── Catalog/
│   │   │   ├── Catalog.Domain/
│   │   │   │   ├── Entities/
│   │   │   │   │   ├── Product.cs
│   │   │   │   │   └── Category.cs
│   │   │   │   ├── ValueObjects/
│   │   │   │   │   ├── Money.cs
│   │   │   │   │   ├── ProductName.cs
│   │   │   │   │   └── SKU.cs
│   │   │   │   ├── Events/
│   │   │   │   │   ├── ProductCreatedEvent.cs
│   │   │   │   │   ├── ProductUpdatedEvent.cs
│   │   │   │   │   └── StockReservedEvent.cs
│   │   │   │   └── Common/
│   │   │   │       ├── BaseEntity.cs
│   │   │   │       ├── IAggregateRoot.cs
│   │   │   │       └── ValueObject.cs
│   │   │   │
│   │   │   ├── Catalog.Application/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreateProduct/
│   │   │   │   │   │   ├── CreateProductCommand.cs (Command + Handler)
│   │   │   │   │   │   ├── CreateProductValidator.cs
│   │   │   │   │   │   └── CreateProductResponse.cs
│   │   │   │   │   ├── UpdateProduct/
│   │   │   │   │   │   ├── UpdateProductCommand.cs (Command + Handler)
│   │   │   │   │   │   ├── UpdateProductValidator.cs
│   │   │   │   │   │   └── UpdateProductResponse.cs
│   │   │   │   │   ├── DeleteProduct/
│   │   │   │   │   │   ├── DeleteProductCommand.cs (Command + Handler)
│   │   │   │   │   │   └── DeleteProductValidator.cs
│   │   │   │   │   └── ReserveStock/
│   │   │   │   │       ├── ReserveStockCommand.cs (Command + Handler)
│   │   │   │   │       ├── ReserveStockValidator.cs
│   │   │   │   │       └── ReserveStockResponse.cs
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetProduct/
│   │   │   │   │   │   ├── GetProductQuery.cs (Query + Handler)
│   │   │   │   │   │   └── GetProductResponse.cs
│   │   │   │   │   ├── GetProducts/
│   │   │   │   │   │   ├── GetProductsQuery.cs (Query + Handler)
│   │   │   │   │   │   └── GetProductsResponse.cs
│   │   │   │   │   └── GetProductsWithPagination/
│   │   │   │   │       ├── GetProductsWithPaginationQuery.cs (Query + Handler)
│   │   │   │   │       └── GetProductsWithPaginationResponse.cs
│   │   │   │   ├── Interfaces/
│   │   │   │   │   └── IApplicationDbContext.cs
│   │   │   │   ├── Mappings/
│   │   │   │   │   └── MappingProfile.cs
│   │   │   │   ├── Behaviors/
│   │   │   │   │   ├── ValidationBehavior.cs
│   │   │   │   │   ├── LoggingBehavior.cs
│   │   │   │   │   └── PerformanceBehavior.cs
│   │   │   │   └── DependencyInjection.cs
│   │   │   │
│   │   │   ├── Catalog.Infrastructure/
│   │   │   │   ├── Persistence/
│   │   │   │   │   ├── CatalogDbContext.cs (implements IApplicationDbContext)
│   │   │   │   │   ├── Configurations/
│   │   │   │   │   │   ├── ProductConfiguration.cs
│   │   │   │   │   │   └── CategoryConfiguration.cs
│   │   │   │   │   └── Migrations/
│   │   │   │   ├── Messaging/
│   │   │   │   │   └── MassTransitConfiguration.cs
│   │   │   │   └── DependencyInjection.cs
│   │   │   │
│   │   │   └── Catalog.API/
│   │   │       ├── Controllers/
│   │   │       │   └── ProductsController.cs
│   │   │       ├── Consumers/
│   │   │       │   └── OrderCreatedConsumer.cs (consumes event, sends to MediatR)
│   │   │       ├── Middleware/
│   │   │       │   └── ExceptionHandlingMiddleware.cs
│   │   │       ├── Program.cs
│   │   │       ├── appsettings.json
│   │   │       ├── appsettings.Development.json
│   │   │       └── Dockerfile
│   │   │
│   │   ├── Order/
│   │   │   ├── Order.Domain/
│   │   │   │   ├── Entities/
│   │   │   │   │   ├── Order. cs
│   │   │   │   │   └── OrderItem.cs
│   │   │   │   ├── ValueObjects/
│   │   │   │   │   ├── Address.cs
│   │   │   │   │   └── OrderStatus.cs (enum)
│   │   │   │   ├── Events/
│   │   │   │   │   ├── OrderCreatedEvent.cs
│   │   │   │   │   ├── OrderConfirmedEvent.cs
│   │   │   │   │   └── OrderCancelledEvent.cs
│   │   │   │   └── Common/
│   │   │   │
│   │   │   ├── Order.Application/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── PlaceOrder/
│   │   │   │   │   │   ├── PlaceOrderCommand.cs (Command + Handler)
│   │   │   │   │   │   ├── PlaceOrderValidator.cs
│   │   │   │   │   │   └── PlaceOrderResponse.cs
│   │   │   │   │   ├── ConfirmOrder/
│   │   │   │   │   │   ├── ConfirmOrderCommand.cs (Command + Handler)
│   │   │   │   │   │   └── ConfirmOrderResponse.cs
│   │   │   │   │   └── CancelOrder/
│   │   │   │   │       ├── CancelOrderCommand.cs (Command + Handler)
│   │   │   │   │       └── CancelOrderValidator.cs
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetOrder/
│   │   │   │   │   │   ├── GetOrderQuery.cs (Query + Handler)
│   │   │   │   │   │   └── GetOrderResponse.cs
│   │   │   │   │   └── GetOrdersByCustomer/
│   │   │   │   │       ├── GetOrdersByCustomerQuery.cs (Query + Handler)
│   │   │   │   │       └── GetOrdersByCustomerResponse.cs
│   │   │   │   ├── Interfaces/
│   │   │   │   │   └── IApplicationDbContext.cs
│   │   │   │   ├── Mappings/
│   │   │   │   │   └── MappingProfile.cs
│   │   │   │   ├── Behaviors/
│   │   │   │   │   ├── ValidationBehavior.cs
│   │   │   │   │   ├── LoggingBehavior.cs
│   │   │   │   │   └── PerformanceBehavior.cs
│   │   │   │   └── DependencyInjection.cs
│   │   │   │
│   │   │   ├── Order.Infrastructure/
│   │   │   │   ├── Persistence/
│   │   │   │   │   ├── OrderDbContext.cs (implements IApplicationDbContext)
│   │   │   │   │   ├── Schemas/
│   │   │   │   │   │   ├── order_schema/
│   │   │   │   │   │   └── product_read_schema/
│   │   │   │   │   ├── Configurations/
│   │   │   │   │   │   ├── OrderConfiguration.cs
│   │   │   │   │   │   ├── OrderItemConfiguration.cs
│   │   │   │   │   │   └── ProductReadModelConfiguration.cs
│   │   │   │   │   └── Migrations/
│   │   │   │   ├── Messaging/
│   │   │   │   │   └── MassTransitConfiguration.cs
│   │   │   │   └── DependencyInjection.cs
│   │   │   │
│   │   │   └── Order.API/
│   │   │       ├── Controllers/
│   │   │       │   └── OrdersController.cs
│   │   │       ├── Consumers/
│   │   │       │   ├── ProductCreatedConsumer.cs (consumes event, sends to MediatR)
│   │   │       │   ├── ProductUpdatedConsumer.cs (consumes event, sends to MediatR)
│   │   │       │   ├── ProductDeletedConsumer.cs (consumes event, sends to MediatR)
│   │   │       │   ├── StockReservedConsumer.cs (consumes event, sends to MediatR)
│   │   │       │   └── StockReservationFailedConsumer.cs (consumes event, sends to MediatR)
│   │   │       ├── Middleware/
│   │   │       │   └── ExceptionHandlingMiddleware.cs
│   │   │       ├── Program.cs
│   │   │       ├── appsettings.json
│   │   │       └── Dockerfile
│   │
│   └── BuildingBlocks/
│       ├── BuildingBlocks.Common/
│       │   ├── Domain/
│       │   │   ├── BaseEntity.cs
│       │   │   ├── BaseAuditableEntity.cs
│       │   │   ├── IAggregateRoot.cs
│       │   │   └── ValueObject.cs
│       │   └── Exceptions/
│       │       ├── DomainException.cs
│       │       ├── NotFoundException.cs
│       │       └── ValidationException.cs
│       │
│       ├── BuildingBlocks.Messaging/
│       │   ├── Events/
│       │   │   ├── ProductCreatedEvent.cs
│       │   │   ├── ProductUpdatedEvent.cs
│       │   │   ├── ProductDeletedEvent.cs
│       │   │   ├── StockReservedEvent.cs
│       │   │   ├── StockReservationFailedEvent.cs
│       │   │   ├── OrderCreatedEvent.cs
│       │   │   ├── OrderConfirmedEvent.cs
│       │   │   └── OrderCancelledEvent.cs
│       │   └── Filters/
│       │       ├── Correlations/
│       │       │   ├── CorrelationConsumeFilter.cs
│       │       │   ├── CorrelationPublishFilter.cs
│       │       │   ├── CorrelationSendFilter.cs
│       │       │   ├── CorrelationHeaderHandler.cs
│       │       │   └── CorrelationMiddleware.cs
│       │       ├── Tokens/
│       │       │   ├── TokenConsumeFilter.cs
│       │       │   ├── TokenPublishFilter.cs
│       │       │   ├── TokenSendFilter.cs
│       │       │   ├── TokenHeaderHandler.cs
│       │       │   └── TokenMiddleware.cs
│       │       └── Localization/
│       │           ├── LocalizationConsumeFilter.cs
│       │           ├── LocalizationPublishFilter.cs
│       │           ├── LocalizationSendFilter.cs
│       │           ├── LocalizationHeaderHandler.cs
│       │           ├── LocalizationMiddleware.cs
│       │           └── LocalizationExtensions.cs
│       │
│       └── BuildingBlocks.Saga/
│           ├── ISagaStep.cs
│           ├── SagaStepBase.cs
│           └── CompensationContext.cs
│
├── tests/
│   ├── Catalog.UnitTests/
│   │   ├── Domain/
│   │   │   ├── ProductTests.cs
│   │   │   └── CategoryTests.cs
│   │   └── Application/
│   │       ├── Commands/
│   │       │   └── CreateProductCommandHandlerTests.cs
│   │       └── Queries/
│   │           └── GetProductQueryHandlerTests.cs
│   │
│   ├── Catalog.IntegrationTests/
│   │   ├── Api/
│   │   │   └── ProductsControllerTests.cs
│   │   ├── Messaging/
│   │   │   └── ProductEventsTests.cs
│   │   └── Infrastructure/
│   │       └── CatalogApiFactory.cs
│   │
│   ├── Order. UnitTests/
│   │   ├── Domain/
│   │   │   └── OrderTests.cs
│   │   └── Application/
│   │       └── Commands/
│   │           └── PlaceOrderCommandHandlerTests.cs
│   │
│   ├── Order.IntegrationTests/
│   │   ├── Api/
│   │   │   └── OrdersControllerTests.cs
│   │   ├── Saga/
│   │   │   └── PlaceOrderChoreographyTests.cs
│   │   └── Messaging/
│   │       └── ProductReadModelSyncTests.cs
│   │
│   ├── ChaosTests/
│   │   ├── NetworkFailureTests.cs
│   │   ├── DatabaseFailureTests.cs
│   │   └── MessageBrokerFailureTests.cs
│   │
│   └── LoadTests/
│       ├── Scenarios/
│       │   ├── NormalLoadScenario.cs
│       │   ├── PeakLoadScenario.cs
│       │   ├── StressTestScenario.cs
│       │   ├── SpikeTestScenario.cs
│       │   └── EnduranceTestScenario.cs
│       └── Program.cs
│
├── infrastructure/
│   ├── docker/
│   │   ├── keycloak/
│   │   │   ├── Dockerfile
│   │   │   └── realm-export.json
│   │   ├── postgres/
│   │   │   └── init-scripts/
│   │   └── rabbitmq/
│   │       └── rabbitmq.conf
│   └── k8s/ (optional)
│       ├── catalog-deployment.yaml
│       ├── order-deployment.yaml
│       └── notification-deployment.yaml
│
├── . github/
│   └── workflows/
│       ├── ci.yml
│       ├── integration-tests.yml
│       ├── chaos-tests.yml
│       └── load-tests.yml
│
├── docker-compose.yml
├── docker-compose.override.yml
├── . gitignore
├── .editorconfig
├── Directory.Build.props
├── Micro.sln
└── README.md
```

---

## 🗄️ Domain Model

### **Catalog Service Domain**

#### **Product (Aggregate Root)**

```csharp
public class Product : BaseAuditableEntity, IAggregateRoot
{
    public Guid Id { get; private set; }
    public ProductName Name { get; private set; }
    public string Description { get; private set; }
    public Money Price { get; private set; }
    public int StockQuantity { get; private set; }
    public SKU Sku { get; private set; }
    public Guid CategoryId { get; private set; }
    
    // Navigation
    public Category Category { get; private set; }
    
    // Domain Events
    private readonly List<object> _domainEvents = new();
    public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();
    
    private Product() { } // EF Core
    
    public static Product Create(ProductName name, string description, Money price, 
                                  int stockQuantity, SKU sku, Guid categoryId)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Price = price,
            StockQuantity = stockQuantity,
            Sku = sku,
            CategoryId = categoryId
        };
        
        product._domainEvents.Add(new ProductCreatedEvent
        {
            Id = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            CorrelationId = string.Empty,
            ProductId = product.Id,
            Name = product.Name.Value,
            Price = product.Price.Amount,
            IsAvailable = product.StockQuantity > 0
        });
        
        return product;
    }
    
    public void Update(ProductName name, string description, Money price)
    {
        Name = name;
        Description = description;
        Price = price;
        
        _domainEvents.Add(new ProductUpdatedEvent
        {
            Id = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            CorrelationId = string.Empty,
            ProductId = Id,
            Name = Name.Value,
            Price = Price.Amount,
            IsAvailable = StockQuantity > 0
        });
    }
    
    public void ReserveStock(int quantity, Guid orderId)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero");
            
        if (StockQuantity < quantity)
            throw new DomainException($"Insufficient stock. Available: {StockQuantity}, Requested: {quantity}");
        
        StockQuantity -= quantity;
        
        _domainEvents.Add(new StockReservedEvent
        {
            Id = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            CorrelationId = string.Empty,
            OrderId = orderId,
            ProductId = Id,
            QuantityReserved = quantity
        });
    }
    
    public void RestoreStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero");
            
        StockQuantity += quantity;
    }
    
    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

#### **Category (Entity)**

```csharp
public class Category : BaseAuditableEntity
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    
    // Navigation
    public ICollection<Product> Products { get; private set; } = new List<Product>();
    
    private Category() { } // EF Core
    
    public static Category Create(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Category name is required");
            
        return new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description
        };
    }
    
    public void Update(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Category name is required");
            
        Name = name;
        Description = description;
    }
}
```

#### **Value Objects**

```csharp
// ProductName.cs
public class ProductName : ValueObject
{
    public string Value { get; private set; }
    
    private ProductName() { }
    
    public static ProductName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Product name is required");
            
        if (value.Length > 200)
            throw new DomainException("Product name cannot exceed 200 characters");
            
        return new ProductName { Value = value };
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

// Money.cs
public class Money : ValueObject
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    
    private Money() { }
    
    public static Money Create(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            throw new DomainException("Price cannot be negative");
            
        return new Money { Amount = amount, Currency = currency };
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}

// SKU.cs
public class SKU : ValueObject
{
    public string Value { get; private set; }
    
    private SKU() { }
    
    public static SKU Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("SKU is required");
            
        if (value.Length > 50)
            throw new DomainException("SKU cannot exceed 50 characters");
            
        return new SKU { Value = value.ToUpperInvariant() };
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
```

---

### **Order Service Domain**

#### **Order (Aggregate Root)**

```csharp
public class Order : BaseAuditableEntity, IAggregateRoot
{
    public Guid Id { get; private set; }
    public string OrderNumber { get; private set; }
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public Address ShippingAddress { get; private set; }
    
    private readonly List<OrderItem> _items = new();
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
    
    // Domain Events
    private readonly List<object> _domainEvents = new();
    public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();
    
    private Order() { } // EF Core
    
    public static Order Create(Guid customerId, Address shippingAddress, List<OrderItem> items)
    {
        if (items == null || !items.Any())
            throw new DomainException("Order must have at least one item");
            
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = GenerateOrderNumber(),
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            ShippingAddress = shippingAddress,
            TotalAmount = items.Sum(i => i.TotalPrice)
        };
        
        order._items.AddRange(items);
        
        order._domainEvents.Add(new OrderCreatedEvent
        {
            Id = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            CorrelationId = string.Empty,
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            Items = order.Items.Select(i => new OrderItemData
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList(),
            TotalAmount = order.TotalAmount
        });
        
        return order;
    }
    
    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException($"Cannot confirm order in {Status} status");
            
        Status = OrderStatus.Confirmed;
        
        _domainEvents.Add(new OrderConfirmedEvent
        {
            Id = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            CorrelationId = string.Empty,
            OrderId = Id,
            CustomerId = CustomerId,
            OrderNumber = OrderNumber,
            TotalAmount = TotalAmount,
            Items = Items.Select(i => new OrderItemData
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList()
        });
    }
    
    public void Cancel(string reason)
    {
        if (Status == OrderStatus.Confirmed)
            throw new DomainException("Cannot cancel confirmed order");
            
        if (Status == OrderStatus.Cancelled)
            throw new DomainException("Order is already cancelled");
            
        Status = OrderStatus.Cancelled;
        
        _domainEvents.Add(new OrderCancelledEvent
        {
            Id = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            CorrelationId = string.Empty,
            OrderId = Id,
            CustomerId = CustomerId,
            Reason = reason
        });
    }
    
    private static string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
    }
    
    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

#### **OrderItem (Entity)**

```csharp
public class OrderItem : BaseEntity
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice { get; private set; }
    
    // Navigation
    public Order Order { get; private set; }
    
    private OrderItem() { } // EF Core
    
    public static OrderItem Create(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero");
            
        if (unitPrice < 0)
            throw new DomainException("Unit price cannot be negative");
            
        return new OrderItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductName = productName,
            Quantity = quantity,
            UnitPrice = unitPrice,
            TotalPrice = quantity * unitPrice
        };
    }
}
```

#### **Value Objects**

```csharp
// Address.cs
public class Address : ValueObject
{
    public string Street { get; private set; }
    public string City { get; private set; }
    public string PostalCode { get; private set; }
    public string Country { get; private set; }
    
    private Address() { }
    
    public static Address Create(string street, string city, string postalCode, string country)
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new DomainException("Street is required");
            
        if (string.IsNullOrWhiteSpace(city))
            throw new DomainException("City is required");
            
        if (string.IsNullOrWhiteSpace(postalCode))
            throw new DomainException("Postal code is required");
            
        if (string.IsNullOrWhiteSpace(country))
            throw new DomainException("Country is required");
            
        return new Address
        {
            Street = street,
            City = city,
            PostalCode = postalCode,
            Country = country
        };
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return PostalCode;
        yield return Country;
    }
}

// OrderStatus.cs (Enum)
public enum OrderStatus
{
    Pending = 1,
    Confirmed = 2,
    Cancelled = 3
}
```

#### **ProductReadModel (for Order Service)**

```csharp
public class ProductReadModel : BaseEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime LastSyncedAt { get; set; }
    
    public void Update(string name, decimal price, bool isAvailable)
    {
        Name = name;
        Price = price;
        IsAvailable = isAvailable;
        LastSyncedAt = DateTime.UtcNow;
    }
}
```

---

### **Base Classes (BuildingBlocks.Common)**

```csharp
// BaseEntity.cs
public abstract class BaseEntity
{
    public Guid Id { get; protected set; }
}

// BaseAuditableEntity.cs
public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}

// IAggregateRoot.cs
public interface IAggregateRoot
{
    IReadOnlyList<object> DomainEvents { get; }
    void ClearDomainEvents();
}

// ValueObject.cs
public abstract class ValueObject
{
    protected abstract IEnumerable<object> GetEqualityComponents();
    
    public override bool Equals(object obj)
    {
        if (obj == null || obj.GetType() != GetType())
            return false;
            
        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }
    
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }
    
    public static bool operator ==(ValueObject left, ValueObject right)
    {
        if (ReferenceEquals(left, null) && ReferenceEquals(right, null))
            return true;
            
        if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
            return false;
            
        return left.Equals(right);
    }
    
    public static bool operator !=(ValueObject left, ValueObject right)
    {
        return !(left == right);
    }
}
```

---

### **Domain Model Diagram**

```
┌─────────────────────────────────────────────────────────┐
│                    CATALOG DOMAIN                        │
└─────────────────────────────────────────────────────────┘

    ┌──────────────────┐
    │    Category      │
    ├──────────────────┤
    │ + Id             │
    │ + Name           │
    │ + Description    │
    └──────────────────┘
            △
            │ 1
            │
            │ *
    ┌──────────────────┐         ┌─────────────────┐
    │ Product (AR)     │────────>│   ProductName   │
    ├──────────────────┤         └─────────────────┘
    │ + Id             │         ┌─────────────────┐
    │ + Name           │────────>│     Money       │
    │ + Description    │         └─────────────────┘
    │ + Price          │         ┌─────────────────┐
    │ + StockQuantity  │────────>│      SKU        │
    │ + Sku            │         └─────────────────┘
    │ + CategoryId     │
    ├──────────────────┤
    │ + Create()       │
    │ + Update()       │
    │ + ReserveStock() │
    │ + RestoreStock() │
    └──────────────────┘

┌─────────────────────────────────────────────────────────┐
│                     ORDER DOMAIN                         │
└─────────────────────────────────────────────────────────┘

    ┌──────────────────┐                    ┌─────────────────┐
    │  Order (AR)      │───────────────────>│    Address      │
    ├──────────────────┤                    └─────────────────┘
    │ + Id             │
    │ + OrderNumber    │         ┌─────────────────┐
    │ + CustomerId     │────────>│  OrderStatus    │
    │ + Status         │         │  - Pending      │
    │ + TotalAmount    │         │  - Confirmed    │
    │ + ShippingAddr   │         │  - Cancelled    │
    │ + Items          │         └─────────────────┘
    ├──────────────────┤
    │ + Create()       │
    │ + Confirm()      │
    │ + Cancel()       │
    └────────┬─────────┘
             │ 1
             │
             │ *
    ┌────────┴─────────┐
    │   OrderItem      │
    ├──────────────────┤
    │ + Id             │
    │ + OrderId        │
    │ + ProductId      │
    │ + ProductName    │
    │ + Quantity       │
    │ + UnitPrice      │
    │ + TotalPrice     │
    └──────────────────┘

    ┌──────────────────┐
    │ ProductReadModel │  (Event Sourcing from Catalog)
    ├──────────────────┤
    │ + Id             │
    │ + Name           │
    │ + Price          │
    │ + IsAvailable    │
    │ + LastSyncedAt   │
    └──────────────────┘
```

---

### **Business Rules & Invariants**

#### **Catalog Service**
1. **Product must have a valid name** (1-200 characters)
2. **SKU must be unique** across all products
3. **Price cannot be negative**
4. **Stock reservation** only allowed if sufficient quantity available
5. **Stock quantity** cannot go below zero
6. **Category name is required**
7. **Product must belong to an existing category**

#### **Order Service**
1. **Order must have at least one item**
2. **Order can only be confirmed from Pending status**
3. **Confirmed orders cannot be cancelled**
4. **Order items quantity must be greater than zero**
5. **Unit price cannot be negative**
6. **Shipping address is required** for all orders
7. **Order number is auto-generated** and unique
8. **Total amount is calculated** from order items

#### **Cross-Service Business Rules**
1. **Product availability check** before order placement
2. **Stock is reserved** when order is created (Pending)
3. **Stock is committed** when order is confirmed
4. **Stock is restored** when order is cancelled
5. **Product read model** in Order service must stay synchronized with Catalog service

---

## 🔌 API Endpoints

### **Catalog Service (Port 5001)**

#### **Products**
```http
GET    /api/v1/products
GET    /api/v1/products/{id}
GET    /api/v1/products/search? name={name}&category={categoryId}&page={page}&size={size}
POST   /api/v1/products
PUT    /api/v1/products/{id}
DELETE /api/v1/products/{id}
PATCH  /api/v1/products/{id}/stock
```

#### **Categories**
```http
GET    /api/v1/categories
GET    /api/v1/categories/{id}
POST   /api/v1/categories
PUT    /api/v1/categories/{id}
DELETE /api/v1/categories/{id}
```

#### **Health**
```http
GET    /health
GET    /health/ready
```

---

### **Order Service (Port 5002)**

#### **Orders**
```http
GET    /api/v1/orders
GET    /api/v1/orders/{id}
GET    /api/v1/orders/customer/{customerId}
POST   /api/v1/orders          # PlaceOrder
GET    /api/v1/orders/{id}/status
```

#### **Health**
```http
GET    /health
GET    /health/ready
```

---

### **API Gateway (Port 5000)**

Routes all requests with prefix: 
```http
/api/catalog/*      → Catalog Service (Port 5001)
/api/orders/*       → Order Service (Port 5002)
```

---

## 🔄 Choreography Saga Flow

### **PlaceOrder Saga - Detailed Flow**

```
┌──────────────────────────────────────────────────────────────────┐
│ Step 1: Create Order (Pending)                                   │
└──────────────────────────────────────────────────────────────────┘

Customer                Order Service
   │                          │
   │  POST /api/orders        │
   │─────────────────────────►│
   │                          │ 1.  Validate request
   │                          │ 2. Create Order (Status: Pending)
   │                          │ 3. Save to order_schema.orders
│                          │ 4. Publish: OrderCreatedEvent
   │                          │ 5. Return 202 Accepted
   │◄─────────────────────────│
   │  { orderId, status }     │

┌──────────────────────────────────────────────────────────────────┐
│ Step 2: Reserve Stock                                            │
└──────────────────────────────────────────────────────────────────┘

Order Service            RabbitMQ            Catalog Service
   │                          │                     │
   │ OrderCreatedEvent        │                     │
   │─────────────────────────►│                     │
   │                          │  OrderCreatedEvent  │
   │                          │────────────────────►│
   │                          │                     │ 1. Consume event
   │                          │                     │ 2. Check stock availability
   │                          │                     │ 3. Deduct stock (HARD)
   │                          │                     │ 4. Save to catalog_db
   │                          │                     │
   │                     ┌────┴─────┐               │
   │                     │          │               │
   │                 SUCCESS      FAILURE           │
   │                     │          │               │
   │                     │          │ 5a. Publish: StockReservationFailedEvent
   │                     │          │               │
   │                     │ 5b.  Publish: StockReservedEvent
   │                     │          │               │

┌──────────────────────────────────────────────────────────────────┐
│ Step 3a: Confirm Order (SUCCESS Path)                           │
└──────────────────────────────────────────────────────────────────┘

Catalog Service          RabbitMQ            Order Service
   │                          │                     │
   │ StockReservedEvent       │                     │
   │─────────────────────────►│                     │
   │                          │  StockReservedEvent │
   │                          │────────────────────►│
   │                          │                     │ 1. Consume event
   │                          │                     │ 2. Update order status:  Confirmed
   │                          │                     │ 3. Save to order_schema.orders
   │                          │                     │ 4. Publish: OrderConfirmedEvent
   │                          │◄────────────────────│

┌──────────────────────────────────────────────────────────────────┐
│ Step 3b: Cancel Order (FAILURE Path - Compensation)             │
└──────────────────────────────────────────────────────────────────┘

Catalog Service          RabbitMQ            Order Service
   │                          │                     │
   │ StockReservationFailedEvent                    │
   │─────────────────────────►│                     │
   │                          │  StockReservationFailedEvent
   │                          │────────────────────►│
   │                          │                     │ 1. Consume event
   │                          │                     │ 2. Update order status: Cancelled
   │                          │                     │ 3. Save reason
   │                          │                     │ 4. Publish: OrderCancelledEvent
   │                          │◄────────────────────│

```

### **Notes:**
- OrderConfirmedEvent and OrderCancelledEvent are published for potential future consumers
- No notification service currently consumes these events
- Can add notification service later without changing existing services

### **Saga State Transitions**

```
Order Status Flow: 

[Customer Request]
        │
        ▼
    ┌─────────┐
    │ Pending │ ← Initial state after POST /api/orders
    └────┬────┘
         │
    ┌────┴────┐
    │         │
SUCCESS    FAILURE
    │         │
    ▼         ▼
┌──────────┐ ┌──────────┐
│Confirmed │ │Cancelled │ ← Terminal states
└──────────┘ └──────────┘
```

---

## 📋 Complete Task Breakdown

### **Week 1: Foundation & BuildingBlocks**

#### **Task 1: Solution Structure & BuildingBlocks**
- Create Micro.sln
- **BuildingBlocks.Common:**
  - Domain: BaseEntity, BaseAuditableEntity, IAggregateRoot, ValueObject
  - Exceptions: DomainException, NotFoundException, ValidationException (NO Result pattern, NO domain-specific exceptions)
- **BuildingBlocks.Messaging:**
  - Events: All event definitions as standalone records (NO base class, NO inheritance)
    - ProductCreatedEvent, ProductUpdatedEvent, ProductDeletedEvent
    - StockReservedEvent, StockReservationFailedEvent
    - OrderCreatedEvent, OrderConfirmedEvent, OrderCancelledEvent
    - Each event contains: Id, OccurredAt, CorrelationId properties
  - **Filters/Correlations/**: CorrelationConsumeFilter, CorrelationPublishFilter, CorrelationSendFilter, CorrelationHeaderHandler, CorrelationMiddleware
  - **Filters/Tokens/**: TokenConsumeFilter, TokenPublishFilter, TokenSendFilter, TokenHeaderHandler, TokenMiddleware
  - **Filters/Localization/**: LocalizationConsumeFilter, LocalizationPublishFilter, LocalizationSendFilter, LocalizationHeaderHandler, LocalizationMiddleware, LocalizationExtensions
  - **Middleware/**: CorrelationIdMiddleware (legacy support)
- **BuildingBlocks.Saga:**
  - ISagaStep, SagaStepBase, CompensationContext
- Directory.Build.props with common package versions

#### **Task 2: Catalog.Domain**
- Entities: Product, Category (with aggregate root logic)
- Value Objects: Money, ProductName, SKU
- Domain Events: ProductCreatedEvent, ProductUpdatedEvent, StockReservedEvent
- Use DomainException from BuildingBlocks.Common for all domain errors

#### **Task 3: Catalog.Application**
- IApplicationDbContext interface
- **Commands (Command + Handler in same file, separate Validator and Response):**
  - CreateProduct/
    - CreateProductCommand.cs (Command + Handler)
    - CreateProductValidator.cs
    - CreateProductResponse.cs
  - UpdateProduct/
    - UpdateProductCommand.cs (Command + Handler)
    - UpdateProductValidator.cs
    - UpdateProductResponse.cs
  - DeleteProduct/
    - DeleteProductCommand.cs (Command + Handler)
    - DeleteProductValidator.cs
  - ReserveStock/
    - ReserveStockCommand.cs (Command + Handler)
    - ReserveStockValidator.cs
    - ReserveStockResponse.cs
- **Queries (Query + Handler in same file, separate Response):**
  - GetProduct/
    - GetProductQuery.cs (Query + Handler)
    - GetProductResponse.cs
  - GetProducts/
    - GetProductsQuery.cs (Query + Handler)
    - GetProductsResponse.cs
  - GetProductsWithPagination/
    - GetProductsWithPaginationQuery.cs (Query + Handler)
    - GetProductsWithPaginationResponse.cs
- Behaviors: ValidationBehavior, LoggingBehavior, PerformanceBehavior
- Mappings: MappingProfile
- DependencyInjection.cs

#### **Task 4: Catalog.Infrastructure**
- CatalogDbContext (implements IApplicationDbContext)
- EF Core Configurations: ProductConfiguration, CategoryConfiguration
- MassTransit setup with AddEntityFrameworkOutbox<CatalogDbContext> (PostgreSQL)
- MassTransitConfiguration.cs (uses Correlation, Token, and Localization filters from BuildingBlocks.Messaging)
- Migrations
- DependencyInjection.cs

#### **Task 5: Catalog.API**
- ProductsController, CategoriesController
- **Consumers (API layer - consume events and send to MediatR):**
  - OrderCreatedConsumer.cs
- Middleware: ExceptionHandlingMiddleware, CorrelationMiddleware (from BuildingBlocks.Messaging/Filters/Correlations)
- Health checks: /health, /health/ready
- Program.cs (register services directly), appsettings.json, Dockerfile

---

### **Week 2: Order Service & Infrastructure**

#### **Task 6: Order.Domain**
- Entities: Order, OrderItem (with state transition logic)
- Value Objects: Address, OrderStatus (enum: Pending, Confirmed, Cancelled)
- Domain Events: OrderCreatedEvent, OrderConfirmedEvent, OrderCancelledEvent
- Use DomainException from BuildingBlocks.Common for all domain errors

#### **Task 7: Order.Application**
- IApplicationDbContext interface
- **Commands (Command + Handler in same file, separate Validator and Response):**
  - PlaceOrder/
    - PlaceOrderCommand.cs (Command + Handler)
    - PlaceOrderValidator.cs
    - PlaceOrderResponse.cs
  - ConfirmOrder/
    - ConfirmOrderCommand.cs (Command + Handler)
    - ConfirmOrderResponse.cs
  - CancelOrder/
    - CancelOrderCommand.cs (Command + Handler)
    - CancelOrderValidator.cs
- **Queries (Query + Handler in same file, separate Response):**
  - GetOrder/
    - GetOrderQuery.cs (Query + Handler)
    - GetOrderResponse.cs
  - GetOrdersByCustomer/
    - GetOrdersByCustomerQuery.cs (Query + Handler)
    - GetOrdersByCustomerResponse.cs
- Behaviors, Mappings, DependencyInjection

#### **Task 8: Order.Infrastructure**
- OrderDbContext (implements IApplicationDbContext)
- Dual schema configuration:
  - order_schema: Orders, OrderItems
  - product_read_schema: ProductReadModel
- EF Core Configurations: OrderConfiguration, OrderItemConfiguration, ProductReadModelConfiguration
- MassTransit setup with AddEntityFrameworkOutbox<OrderDbContext> (PostgreSQL)
- MassTransitConfiguration.cs (uses Correlation, Token, and Localization filters from BuildingBlocks.Messaging)
- Migrations, DependencyInjection.cs

#### **Task 9: Order.API**
- OrdersController
- **Consumers (API layer - consume events and send to MediatR):**
  - ProductCreatedConsumer.cs (sync to product_read_schema)
  - ProductUpdatedConsumer.cs
  - ProductDeletedConsumer.cs
  - StockReservedConsumer.cs (confirm order)
  - StockReservationFailedConsumer.cs (cancel order)
- Middleware: ExceptionHandlingMiddleware, CorrelationMiddleware (from BuildingBlocks.Messaging/Filters/Correlations)
- Health checks
- Program.cs (register services directly), appsettings.json, Dockerfile

#### **Task 10: API Gateway**
- YARP configuration (yarp.json)
- Routes:
  - /api/catalog/* → Catalog Service (5001)
  - /api/orders/* → Order Service (5002)
- Keycloak authentication integration
- Rate limiting
- Program.cs, appsettings.json, Dockerfile

---

### **Week 3: Infrastructure & Testing**

#### **Task 11: Docker Compose Infrastructure**
- **Services:**
  - PostgreSQL: catalog_db, order_db (ports 5432, 5433)
  - RabbitMQ: ports 5672, 15672
  - Keycloak: port 8080
  - Catalog API: port 5001
  - Order API: port 5002
  - API Gateway: port 5000
- docker-compose.yml, docker-compose.override.yml
- Init scripts for database schemas
- Health check configurations

#### **Task 12: Keycloak Configuration**
- realm-export.json:
  - Realm: microservices
  - Clients: catalog-service, order-service, api-gateway
  - Roles: catalog-admin, order-admin, customer
  - Test users with credentials
- Dockerfile for auto-import

#### **Task 13: Catalog Unit Tests**
- **Domain tests:**
  - Product entity tests (stock reservation, validation)
  - Category entity tests
- **Application tests (Command/Query + Handler, separate Validators and Responses):**
  - CreateProduct handler tests
  - CreateProductValidator tests
  - UpdateProduct handler tests
  - ReserveStock handler tests
  - GetProduct handler tests
- Mock IApplicationDbContext with NSubstitute
- FluentAssertions for readable assertions
- Exception-based error handling tests
- Target: 80%+ coverage with Coverlet

#### **Task 14: Order Unit Tests**
- **Domain tests:**
  - Order entity tests (state transitions: Pending → Confirmed/Cancelled)
  - OrderItem tests
- **Application tests (Command/Query + Handler, separate Validators and Responses):**
  - PlaceOrder handler tests
  - PlaceOrderValidator tests
  - ConfirmOrder handler tests
  - CancelOrder handler tests
  - GetOrder handler tests
- Mock IApplicationDbContext
- Exception-based error handling tests
- Target: 80%+ coverage

---

### **Week 4: Integration & Advanced Testing**

#### **Task 15: Catalog Integration Tests**
- WebApplicationFactory setup
- Testcontainers: PostgreSQL, RabbitMQ
- **Tests:**
  - POST /api/v1/products (create)
  - GET /api/v1/products/{id}
  - PUT /api/v1/products/{id}
  - DELETE /api/v1/products/{id}
  - Event publishing verification (ProductCreatedEvent, etc.)
- Database integration verification

#### **Task 16: Order Integration Tests**
- Testcontainers setup
- **API tests:**
  - POST /api/v1/orders (place order)
  - GET /api/v1/orders/{id}
  - GET /api/v1/orders/customer/{customerId}
- **Saga choreography tests:**
  - Success flow: OrderCreated → StockReserved → OrderConfirmed
  - Failure flow: OrderCreated → StockReservationFailed → OrderCancelled
- **Event consumer tests:**
  - Product read model synchronization (ProductCreated/Updated/Deleted)

#### **Task 17: Chaos Engineering Tests**
- Simmy + Polly setup
- **Scenarios:**
  - Network failures between services (latency, timeouts)
  - PostgreSQL connection failures
  - RabbitMQ unavailability
  - Partial system failures
- Circuit breaker verification
- Retry policy verification
- Graceful degradation tests

#### **Task 18: Load Tests (NBomber)**
- **Scenarios:**
  - NormalLoadScenario: 100 RPS baseline
  - PeakLoadScenario: 200 RPS (2x normal)
  - StressTestScenario: find breaking point (incrementing load)
  - SpikeTestScenario: sudden 500 RPS spike
  - EnduranceTestScenario: sustained 100 RPS for 30 minutes
- **Target endpoints:**
  - POST /api/v1/orders
  - GET /api/v1/products
- Performance reports: response times, throughput, error rates

#### **Task 19: CI/CD - Build & Test**
- **.github/workflows/ci.yml:**
  - Trigger: PR to main
  - Build all projects
  - Run unit tests
  - Code coverage (Coverlet) with 80% threshold
  - Publish test results
- **.github/workflows/integration-tests.yml:**
  - Spin up Docker Compose
  - Run integration tests
  - Tear down infrastructure
  - Publish results

#### **Task 20: CI/CD - Advanced Testing**
- **.github/workflows/chaos-tests.yml:**
  - Schedule: nightly runs
  - Chaos engineering test execution
  - Artifact storage for reports
- **.github/workflows/load-tests.yml:**
  - Trigger: manual or weekly schedule
  - NBomber execution
  - Performance regression detection
  - Slack/email notifications on failures

#### **Task 21: Documentation**
- **README.md:**
  - Project overview
  - Architecture diagram (ASCII + Mermaid)
  - Quick start: `docker-compose up`
  - API documentation with example requests
  - Event catalog reference
  - Testing instructions (unit/integration/chaos/load)
  - Troubleshooting guide
  - Contribution guidelines
- **Architecture.md:** (this file)
- **API.md:** Detailed API documentation with cURL examples

---

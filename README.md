# Micro E-Commerce Platform

A microservices-based e-commerce platform built with .NET 10.0, Clean Architecture, and Domain-Driven Design principles.

## Architecture Overview

This project implements a microservices architecture with the following services:

- **Catalog Service**: Manages products and categories
- **Order Service**: Handles order processing and management

### Technology Stack

- **.NET 10.0** - Target framework
- **MassTransit 8.5.7** - Message broker abstraction with RabbitMQ
- **MediatR 12.5.0** - CQRS implementation
- **FluentValidation 12.1.1** - Request validation
- **Entity Framework Core 10.0.0** - ORM with PostgreSQL
- **Serilog 4.3.0** - Structured logging

### Project Structure

```
Micro/
├── src/
│   ├── BuildingBlocks/
│   │   ├── BuildingBlocks.Common/       # Shared domain primitives & exceptions
│   │   └── BuildingBlocks.Messaging/    # Events, filters, and models
│   └── Services/
│       ├── Catalog/
│       │   ├── Catalog.Domain/          # Entities, value objects
│       │   ├── Catalog.Application/     # CQRS commands, queries, behaviors
│       │   ├── Catalog.Infrastructure/  # EF Core, MassTransit consumers
│       │   └── Catalog.API/             # REST API
│       └── Order/
│           ├── Order.Domain/
│           ├── Order.Application/
│           ├── Order.Infrastructure/
│           └── Order.API/
├── docker-compose.yml
├── Directory.Build.props
└── Micro.sln
```

## Getting Started

### Prerequisites

- .NET 10.0 SDK
- Docker & Docker Compose
- PostgreSQL 16+ (or use Docker)
- RabbitMQ (or use Docker)

### Running with Docker Compose

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop all services
docker-compose down
```

### Running Locally

1. Start infrastructure:
```bash
docker-compose up -d catalog-db order-db rabbitmq
```

2. Run Catalog API:
```bash
cd src/Services/Catalog/Catalog.API
dotnet run
```

3. Run Order API (in another terminal):
```bash
cd src/Services/Order/Order.API
dotnet run
```

### API Endpoints

#### Catalog API (http://localhost:5100)
- `GET /api/products` - Get all products
- `GET /api/products/{id}` - Get product by ID
- `GET /api/products/paginated` - Get products with pagination
- `POST /api/products` - Create a product
- `PUT /api/products/{id}` - Update a product
- `DELETE /api/products/{id}` - Delete a product

#### Order API (http://localhost:5200)
- `GET /api/orders/{id}` - Get order by ID
- `GET /api/orders/customer/{customerId}` - Get orders by customer
- `POST /api/orders` - Create an order
- `POST /api/orders/{id}/confirm` - Confirm an order
- `POST /api/orders/{id}/cancel` - Cancel an order

## Event Flow

1. **Order Created**: When an order is created, `OrderCreatedEvent` is published
2. **Stock Reserved**: Catalog service reserves stock and publishes `StockReservedEvent`
3. **Order Confirmed**: Order service updates order status
4. **Stock Reservation Failed**: If stock is insufficient, `StockReservationFailedEvent` is published
5. **Order Cancelled**: If order is cancelled, `OrderCancelledEvent` triggers stock restoration

## Key Features

- **Clean Architecture**: Domain-centric design with clear separation of concerns
- **CQRS Pattern**: Commands and queries are separated using MediatR
- **Domain Events**: Changes are communicated through domain events
- **MassTransit Inbox/Outbox**: Reliable message delivery with PostgreSQL storage
- **Correlation Tracking**: Request correlation across services
- **Validation**: FluentValidation for request validation
- **Exception Handling**: Global exception handling middleware

## Configuration

### Connection Strings

Catalog API (`appsettings.json`):
```json
{
  "ConnectionStrings": {
    "CatalogDb": "Host=localhost;Port=5432;Database=catalog_db;Username=postgres;Password=postgres",
    "RabbitMq": "amqp://guest:guest@localhost:5672"
  }
}
```

Order API (`appsettings.json`):
```json
{
  "ConnectionStrings": {
    "OrderDb": "Host=localhost;Port=5432;Database=order_db;Username=postgres;Password=postgres",
    "RabbitMq": "amqp://guest:guest@localhost:5672"
  }
}
```

## License

MIT

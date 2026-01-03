# Micro-Commerce Load Tests

This project contains load tests for the Micro-Commerce microservices architecture using [NBomber](https://nbomber.com/).

## Prerequisites

- .NET 10.0 SDK
- Running instances of:
  - Catalog API (default: http://localhost:5001)
  - Order API (default: http://localhost:5002)
  - Gateway (default: http://localhost:5000)

## Running Load Tests

### Basic Usage

```bash
# Run normal load test (100 RPS for 2 minutes)
dotnet run normal

# Run with a specific variant
dotnet run normal orders

# Run peak load test
dotnet run peak

# Run stress test
dotnet run stress
```

### Available Scenarios

| Scenario    | Description                               | Variants                                                      |
| ----------- | ----------------------------------------- | ------------------------------------------------------------- |
| `normal`    | 100 RPS baseline for 2 minutes            | `default`, `orders`                                           |
| `peak`      | 200 RPS (2x normal) for 3 minutes         | `default`, `rampup`                                           |
| `stress`    | Incrementing load to find breaking point  | `default`, `writes`, `concurrent`                             |
| `spike`     | Sudden 500 RPS spike                      | `default`, `double`, `gradual`                                |
| `endurance` | Sustained 100 RPS for 30 minutes          | `default`, `short`, `writes`, `concurrent`                    |
| `write`     | Write operations (CREATE, UPDATE, DELETE) | `products`, `categories`, `orders`, `mixed`, `heavy`, `burst` |

### Scenario Details

#### Normal Load (`normal`)
- **default**: Basic read operations at 100 RPS
- **orders**: Mix of catalog reads and order queries

#### Peak Load (`peak`)
- **default**: Constant 200 RPS load
- **rampup**: Gradual ramp up to 200 RPS, hold, then ramp down

#### Stress Test (`stress`)
- **default**: Increments from 50 to 500 RPS in steps
- **writes**: Includes order creation (write operations)
- **concurrent**: Uses concurrent virtual users pattern

#### Spike Test (`spike`)
- **default**: Baseline → 500 RPS spike → Recovery
- **double**: Two consecutive spikes with recovery periods
- **gradual**: Gradual ramp to spike, then gradual recovery

#### Endurance Test (`endurance`)
- **default**: 30-minute sustained load at 100 RPS
- **short**: 10-minute version for CI/CD
- **writes**: Includes 5% write operations
- **concurrent**: 50 concurrent users for 20 minutes

#### Write Operations (`write`)
- **products**: Full CRUD cycle for products (create → update → delete)
- **categories**: Full CRUD cycle for categories
- **orders**: Full CRUD cycle for orders (create → update status → cancel)
- **mixed**: Realistic 70% reads / 30% writes ratio
- **heavy**: Write-heavy scenario (80% writes / 20% reads)
- **burst**: Flash sale simulation with sudden order burst

## Configuration

### Environment Variables

```bash
# Override API URLs
export CATALOG_API_URL=http://localhost:5001
export ORDER_API_URL=http://localhost:5002
export GATEWAY_URL=http://localhost:5000
```

### appsettings.json

```json
{
  "BaseUrls": {
    "CatalogApi": "http://localhost:5001",
    "OrderApi": "http://localhost:5002",
    "Gateway": "http://localhost:5000"
  },
  "TestData": {
    "CustomerId": "test-customer-id",
    "ProductId": "test-product-id",
    "CategoryId": "test-category-id"
  },
  "Reporting": {
    "ReportFolder": "./reports",
    "ReportFileName": "load_test_report"
  }
}
```

## Reports

After each test run, reports are generated in the `./reports` folder:
- `load_test_report.txt` - Text summary
- `load_test_report.html` - Interactive HTML report
- `load_test_report.csv` - CSV data for further analysis
- `load_test_report.md` - Markdown report

## Performance Metrics

The tests collect the following metrics:
- **Response Time**: Min, Max, Mean, 50th, 75th, 95th, 99th percentiles
- **Throughput**: Requests per second
- **Error Rate**: Failed requests percentage
- **Latency Distribution**: Histogram of response times
- **Step-level Metrics**: Individual step performance

## Target Endpoints

### Catalog API (Read Operations)
- `GET /api/products` - List products
- `GET /api/products/{id}` - Get product details
- `GET /api/categories` - List categories

### Catalog API (Write Operations)
- `POST /api/products` - Create product
- `PUT /api/products/{id}` - Update product
- `DELETE /api/products/{id}` - Delete product
- `POST /api/categories` - Create category
- `PUT /api/categories/{id}` - Update category
- `DELETE /api/categories/{id}` - Delete category

### Order API (Read Operations)
- `GET /api/orders/customer/{customerId}` - Get customer orders

### Order API (Write Operations)
- `POST /api/orders` - Create new order
- `PUT /api/orders/{id}/status` - Update order status
- `DELETE /api/orders/{id}` - Cancel order

## Tips for Effective Load Testing

1. **Warm up the system** before running tests
2. **Monitor system resources** (CPU, Memory, Network)
3. **Run tests multiple times** to get consistent results
4. **Start with lower loads** and gradually increase
5. **Analyze bottlenecks** using the detailed reports

## Integration with CI/CD

Use the short endurance test for CI/CD pipelines:

```bash
dotnet run endurance short
```

This runs a 10-minute test that's suitable for automated pipelines.

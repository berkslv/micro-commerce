using Order.Application.Features.Orders.Queries.GetOrdersByCustomer;
using Order.Application.Interfaces;
using Order.Domain.ValueObjects;
using MockQueryable.Moq;
using OrderEntity = Order.Domain.Entities.Order;

namespace Order.UnitTests.Features.Orders.Queries.GetOrdersByCustomer;

public class GetOrdersByCustomerQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly GetOrdersByCustomerQueryHandler _handler;

    public GetOrdersByCustomerQueryHandlerTests()
    {
        _mockDbContext = new Mock<IApplicationDbContext>();
        _handler = new GetOrdersByCustomerQueryHandler(_mockDbContext.Object);
    }

    [Fact]
    public async Task Handle_WithExistingOrders_ShouldReturnOrderList()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var orders = CreateTestOrdersForCustomer(customerId, 3);
        var query = new GetOrdersByCustomerQuery(customerId);

        SetupMockDbContext(orders);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WithNoOrders_ShouldReturnEmptyList()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var query = new GetOrdersByCustomerQuery(customerId);

        SetupMockDbContext();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldOnlyReturnOrdersForSpecifiedCustomer()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();

        var customerOrders = CreateTestOrdersForCustomer(customerId, 2);
        var otherOrders = CreateTestOrdersForCustomer(otherCustomerId, 3);
        var allOrders = customerOrders.Concat(otherOrders).ToList();

        var query = new GetOrdersByCustomerQuery(customerId);

        SetupMockDbContext(allOrders);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldReturnOrdersWithCorrectData()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var orders = CreateTestOrdersForCustomer(customerId, 1);
        var query = new GetOrdersByCustomerQuery(customerId);

        SetupMockDbContext(orders);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        var orderResponse = result.First();
        orderResponse.Status.Should().Be("Pending");
        orderResponse.TotalAmount.Should().BeGreaterThan(0);
        orderResponse.Currency.Should().Be("USD");
    }

    #region Helper Methods

    private void SetupMockDbContext(List<OrderEntity>? orders = null)
    {
        orders ??= new List<OrderEntity>();
        var mockDbSet = orders.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Orders).Returns(mockDbSet.Object);
    }

    private static List<OrderEntity> CreateTestOrdersForCustomer(Guid customerId, int count)
    {
        var orders = new List<OrderEntity>();
        for (int i = 0; i < count; i++)
        {
            var address = Address.Create("123 Main St", "New York", "NY", "USA", "10001");
            var order = OrderEntity.Create(customerId, "customer@example.com", address, $"Order {i + 1}");
            order.AddItem(Guid.NewGuid(), "Test Product", Money.Create(99.99m, "USD"), 1);
            orders.Add(order);
        }
        return orders;
    }

    #endregion
}

using BuildingBlocks.Common.Exceptions;
using Order.Application.Features.Orders.Queries.GetOrder;
using Order.Application.Interfaces;
using Order.Domain.Entities;
using Order.Domain.ValueObjects;
using MockQueryable.Moq;
using OrderEntity = Order.Domain.Entities.Order;

namespace Order.UnitTests.Features.Orders.Queries.GetOrder;

public class GetOrderQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly GetOrderQueryHandler _handler;

    public GetOrderQueryHandlerTests()
    {
        _mockDbContext = new Mock<IApplicationDbContext>();
        _handler = new GetOrderQueryHandler(_mockDbContext.Object);
    }

    [Fact]
    public async Task Handle_WithExistingOrder_ShouldReturnOrderResponse()
    {
        // Arrange
        var order = CreateTestOrder();
        var orderId = order.Id;
        var query = new GetOrderQuery(orderId);

        SetupMockDbContext(order);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(orderId);
        result.CustomerEmail.Should().Be("customer@example.com");
        result.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task Handle_WithNonExistingOrder_ShouldThrowNotFoundException()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        var query = new GetOrderQuery(nonExistingId);

        SetupMockDbContext();

        // Act
        var act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*Order*{nonExistingId}*");
    }

    [Fact]
    public async Task Handle_WithOrderItems_ShouldReturnOrderWithItems()
    {
        // Arrange
        var order = CreateTestOrderWithMultipleItems();
        var query = new GetOrderQuery(order.Id);

        SetupMockDbContext(order);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldIncludeShippingAddress()
    {
        // Arrange
        var order = CreateTestOrder();
        var query = new GetOrderQuery(order.Id);

        SetupMockDbContext(order);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShippingAddress.Should().NotBeNullOrEmpty();
        result.ShippingAddress.Should().Contain("123 Main St");
        result.ShippingAddress.Should().Contain("New York");
    }

    [Fact]
    public async Task Handle_ShouldCalculateTotalAmount()
    {
        // Arrange
        var order = CreateTestOrder();
        var query = new GetOrderQuery(order.Id);

        SetupMockDbContext(order);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalAmount.Should().Be(199.98m); // 2 * 99.99
    }

    #region Helper Methods

    private void SetupMockDbContext(OrderEntity? order = null)
    {
        var orders = order != null ? new List<OrderEntity> { order } : new List<OrderEntity>();
        var mockOrderDbSet = orders.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Orders).Returns(mockOrderDbSet.Object);

        // Setup empty products DbSet for the join query
        var products = new List<Product>();
        var mockProductDbSet = products.AsQueryable().BuildMockDbSet();
        _mockDbContext.Setup(x => x.Products).Returns(mockProductDbSet.Object);
    }

    private static OrderEntity CreateTestOrder()
    {
        var address = Address.Create("123 Main St", "New York", "NY", "USA", "10001");
        var order = OrderEntity.Create(Guid.NewGuid(), "customer@example.com", address, "Test notes");
        order.AddItem(Guid.NewGuid(), "Test Product", Money.Create(99.99m, "USD"), 2);
        return order;
    }

    private static OrderEntity CreateTestOrderWithMultipleItems()
    {
        var address = Address.Create("123 Main St", "New York", "NY", "USA", "10001");
        var order = OrderEntity.Create(Guid.NewGuid(), "customer@example.com", address, "Test notes");
        order.AddItem(Guid.NewGuid(), "Product 1", Money.Create(10.00m, "USD"), 1);
        order.AddItem(Guid.NewGuid(), "Product 2", Money.Create(20.00m, "USD"), 2);
        return order;
    }

    #endregion
}

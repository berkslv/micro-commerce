using BuildingBlocks.Common.Exceptions;
using BuildingBlocks.Messaging.Events;
using Order.Domain.Entities;
using Order.Domain.Enums;
using Order.Domain.ValueObjects;

namespace Order.UnitTests.Domain;

public class OrderTests
{
    #region Create Tests

    [Fact]
    public void Create_WithValidParameters_ShouldCreateOrder()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var customerEmail = "customer@example.com";
        var shippingAddress = CreateTestAddress();

        // Act
        var order = Order.Domain.Entities.Order.Create(customerId, customerEmail, shippingAddress, "Test notes");

        // Assert
        order.Should().NotBeNull();
        order.Id.Should().NotBeEmpty();
        order.CustomerId.Should().Be(customerId);
        order.CustomerEmail.Should().Be(customerEmail);
        order.ShippingAddress.Should().Be(shippingAddress);
        order.Status.Should().Be(OrderStatus.Pending);
        order.TotalAmount.Amount.Should().Be(0);
        order.Notes.Should().Be("Test notes");
        order.Items.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithEmptyCustomerId_ShouldThrowDomainException()
    {
        // Arrange
        var shippingAddress = CreateTestAddress();

        // Act
        var act = () => Order.Domain.Entities.Order.Create(Guid.Empty, "customer@example.com", shippingAddress);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Customer ID is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidCustomerEmail_ShouldThrowDomainException(string? invalidEmail)
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var shippingAddress = CreateTestAddress();

        // Act
        var act = () => Order.Domain.Entities.Order.Create(customerId, invalidEmail!, shippingAddress);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Customer email is required.");
    }

    [Fact]
    public void Create_WithNullNotes_ShouldCreateOrderWithNullNotes()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var shippingAddress = CreateTestAddress();

        // Act
        var order = Order.Domain.Entities.Order.Create(customerId, "customer@example.com", shippingAddress, null);

        // Assert
        order.Notes.Should().BeNull();
    }

    #endregion

    #region AddItem Tests

    [Fact]
    public void AddItem_WithValidParameters_ShouldAddItemAndUpdateTotal()
    {
        // Arrange
        var order = CreateTestOrder();
        var productId = Guid.NewGuid();
        var unitPrice = Money.Create(25.00m, "USD");

        // Act
        order.AddItem(productId, "Product 1", unitPrice, 2);

        // Assert
        order.Items.Should().HaveCount(1);
        order.Items[0].ProductId.Should().Be(productId);
        order.Items[0].ProductName.Should().Be("Product 1");
        order.Items[0].Quantity.Should().Be(2);
        order.TotalAmount.Amount.Should().Be(50.00m);
    }

    [Fact]
    public void AddItem_WithExistingProduct_ShouldUpdateQuantity()
    {
        // Arrange
        var order = CreateTestOrder();
        var productId = Guid.NewGuid();
        var unitPrice = Money.Create(10.00m, "USD");

        // Act
        order.AddItem(productId, "Product 1", unitPrice, 2);
        order.AddItem(productId, "Product 1", unitPrice, 3);

        // Assert
        order.Items.Should().HaveCount(1);
        order.Items[0].Quantity.Should().Be(5);
        order.TotalAmount.Amount.Should().Be(50.00m);
    }

    [Fact]
    public void AddItem_WithMultipleProducts_ShouldCalculateTotalCorrectly()
    {
        // Arrange
        var order = CreateTestOrder();
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();

        // Act
        order.AddItem(productId1, "Product 1", Money.Create(20.00m, "USD"), 2); // 40
        order.AddItem(productId2, "Product 2", Money.Create(15.00m, "USD"), 3); // 45

        // Assert
        order.Items.Should().HaveCount(2);
        order.TotalAmount.Amount.Should().Be(85.00m);
    }

    #endregion

    #region RemoveItem Tests

    [Fact]
    public void RemoveItem_WithExistingProduct_ShouldRemoveItemAndUpdateTotal()
    {
        // Arrange
        var order = CreateTestOrder();
        var productId = Guid.NewGuid();
        order.AddItem(productId, "Product 1", Money.Create(25.00m, "USD"), 2);

        // Act
        order.RemoveItem(productId);

        // Assert
        order.Items.Should().BeEmpty();
        order.TotalAmount.Amount.Should().Be(0);
    }

    [Fact]
    public void RemoveItem_WithNonExistingProduct_ShouldThrowDomainException()
    {
        // Arrange
        var order = CreateTestOrder();
        var nonExistingProductId = Guid.NewGuid();

        // Act
        var act = () => order.RemoveItem(nonExistingProductId);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage($"*{nonExistingProductId}*not found*");
    }

    #endregion

    #region Submit Tests

    [Fact]
    public void Submit_WithItems_ShouldAddOrderCreatedEvent()
    {
        // Arrange
        var order = CreateTestOrder();
        order.AddItem(Guid.NewGuid(), "Product 1", Money.Create(50.00m, "USD"), 1);
        var correlationId = Guid.NewGuid().ToString();

        // Act
        order.Submit(correlationId);

        // Assert
        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderCreatedEvent>();

        var domainEvent = (OrderCreatedEvent)order.DomainEvents.First();
        domainEvent.OrderId.Should().Be(order.Id);
        domainEvent.CustomerId.Should().Be(order.CustomerId);
        domainEvent.CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public void Submit_WithNoItems_ShouldThrowDomainException()
    {
        // Arrange
        var order = CreateTestOrder();

        // Act
        var act = () => order.Submit(Guid.NewGuid().ToString());

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Cannot submit an order without items.");
    }

    [Fact]
    public void Submit_WhenNotPending_ShouldThrowDomainException()
    {
        // Arrange
        var order = CreateTestOrderWithItems();
        order.Submit(Guid.NewGuid().ToString());
        order.MarkStockReserved();

        // Act
        var act = () => order.Submit(Guid.NewGuid().ToString());

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Only pending orders can be submitted.");
    }

    #endregion

    #region MarkStockReserved Tests

    [Fact]
    public void MarkStockReserved_WhenPending_ShouldUpdateStatus()
    {
        // Arrange
        var order = CreateTestOrderWithItems();
        order.Submit(Guid.NewGuid().ToString());

        // Act
        order.MarkStockReserved();

        // Assert
        order.Status.Should().Be(OrderStatus.StockReserved);
    }

    [Fact]
    public void MarkStockReserved_WhenNotPending_ShouldThrowDomainException()
    {
        // Arrange
        var order = CreateTestOrderWithItems();
        order.Submit(Guid.NewGuid().ToString());
        order.MarkStockReserved();

        // Act
        var act = () => order.MarkStockReserved();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Only pending orders can have stock reserved.");
    }

    #endregion

    #region MarkStockReservationFailed Tests

    [Fact]
    public void MarkStockReservationFailed_WhenPending_ShouldUpdateStatusAndNotes()
    {
        // Arrange
        var order = CreateTestOrderWithItems();
        order.Submit(Guid.NewGuid().ToString());
        var reason = "Insufficient stock";

        // Act
        order.MarkStockReservationFailed(reason);

        // Assert
        order.Status.Should().Be(OrderStatus.StockReservationFailed);
        order.Notes.Should().Contain(reason);
    }

    #endregion

    #region Confirm Tests

    [Fact]
    public void Confirm_WhenStockReserved_ShouldUpdateStatusAndAddEvent()
    {
        // Arrange
        var order = CreateTestOrderWithItems();
        order.Submit(Guid.NewGuid().ToString());
        order.ClearDomainEvents();
        order.MarkStockReserved();
        var correlationId = Guid.NewGuid().ToString();

        // Act
        order.Confirm(correlationId);

        // Assert
        order.Status.Should().Be(OrderStatus.Confirmed);
        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderConfirmedEvent>();
    }

    [Fact]
    public void Confirm_WhenNotStockReserved_ShouldThrowDomainException()
    {
        // Arrange
        var order = CreateTestOrderWithItems();
        order.Submit(Guid.NewGuid().ToString());

        // Act
        var act = () => order.Confirm(Guid.NewGuid().ToString());

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Only orders with reserved stock can be confirmed.");
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public void Cancel_WhenStockReserved_ShouldUpdateStatusAndAddEvent()
    {
        // Arrange
        var order = CreateTestOrderWithItems();
        order.Submit(Guid.NewGuid().ToString());
        order.ClearDomainEvents();
        order.MarkStockReserved();
        var correlationId = Guid.NewGuid().ToString();

        // Act
        order.Cancel("Customer request", correlationId);

        // Assert
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.Notes.Should().Contain("Customer request");
        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderCancelledEvent>();
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ShouldThrowDomainException()
    {
        // Arrange
        var order = CreateTestOrderWithItems();
        order.Submit(Guid.NewGuid().ToString());
        order.MarkStockReserved();
        order.Cancel("First cancellation", Guid.NewGuid().ToString());

        // Act
        var act = () => order.Cancel("Second cancellation", Guid.NewGuid().ToString());

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Order is already cancelled.");
    }

    [Fact]
    public void Cancel_WhenPending_ShouldNotAddCancelledEvent()
    {
        // Arrange
        var order = CreateTestOrderWithItems();
        order.Submit(Guid.NewGuid().ToString());
        order.ClearDomainEvents();

        // Act
        order.Cancel("Cancel before stock reservation", Guid.NewGuid().ToString());

        // Assert
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.DomainEvents.Should().BeEmpty(); // No event because stock was not reserved
    }

    #endregion

    #region State Transition Tests

    [Fact]
    public void MarkAsProcessing_WhenConfirmed_ShouldUpdateStatus()
    {
        // Arrange
        var order = CreateConfirmedOrder();

        // Act
        order.MarkAsProcessing();

        // Assert
        order.Status.Should().Be(OrderStatus.Processing);
    }

    [Fact]
    public void MarkAsProcessing_WhenNotConfirmed_ShouldThrowDomainException()
    {
        // Arrange
        var order = CreateTestOrderWithItems();
        order.Submit(Guid.NewGuid().ToString());
        order.MarkStockReserved();

        // Act
        var act = () => order.MarkAsProcessing();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Only confirmed orders can be processed.");
    }

    [Fact]
    public void MarkAsShipped_WhenProcessing_ShouldUpdateStatus()
    {
        // Arrange
        var order = CreateConfirmedOrder();
        order.MarkAsProcessing();

        // Act
        order.MarkAsShipped();

        // Assert
        order.Status.Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public void MarkAsDelivered_WhenShipped_ShouldUpdateStatus()
    {
        // Arrange
        var order = CreateConfirmedOrder();
        order.MarkAsProcessing();
        order.MarkAsShipped();

        // Act
        order.MarkAsDelivered();

        // Assert
        order.Status.Should().Be(OrderStatus.Delivered);
    }

    #endregion

    #region ClearDomainEvents Tests

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var order = CreateTestOrderWithItems();
        order.Submit(Guid.NewGuid().ToString());
        order.DomainEvents.Should().NotBeEmpty();

        // Act
        order.ClearDomainEvents();

        // Assert
        order.DomainEvents.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private static Address CreateTestAddress()
    {
        return Address.Create(
            "123 Main St",
            "New York",
            "NY",
            "USA",
            "10001");
    }

    private static Order.Domain.Entities.Order CreateTestOrder()
    {
        return Order.Domain.Entities.Order.Create(
            Guid.NewGuid(),
            "customer@example.com",
            CreateTestAddress(),
            null);
    }

    private static Order.Domain.Entities.Order CreateTestOrderWithItems()
    {
        var order = CreateTestOrder();
        order.AddItem(Guid.NewGuid(), "Product 1", Money.Create(50.00m, "USD"), 1);
        return order;
    }

    private static Order.Domain.Entities.Order CreateConfirmedOrder()
    {
        var order = CreateTestOrderWithItems();
        order.Submit(Guid.NewGuid().ToString());
        order.MarkStockReserved();
        order.Confirm(Guid.NewGuid().ToString());
        return order;
    }

    #endregion
}

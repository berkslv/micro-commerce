using Catalog.Application.Models;

namespace Catalog.UnitTests.Models;

public class PaginatedListTests
{
    [Fact]
    public void Constructor_ShouldSetItemsCorrectly()
    {
        // Arrange
        var items = new List<string> { "Item1", "Item2", "Item3" };

        // Act
        var result = new PaginatedList<string>(items, 10, 1, 5);

        // Assert
        result.Items.Should().BeEquivalentTo(items);
    }

    [Fact]
    public void Constructor_ShouldSetTotalCountCorrectly()
    {
        // Arrange
        var items = new List<string> { "Item1", "Item2" };

        // Act
        var result = new PaginatedList<string>(items, 50, 1, 10);

        // Assert
        result.TotalCount.Should().Be(50);
    }

    [Fact]
    public void Constructor_ShouldSetPageNumberCorrectly()
    {
        // Arrange
        var items = new List<string> { "Item1", "Item2" };

        // Act
        var result = new PaginatedList<string>(items, 50, 3, 10);

        // Assert
        result.PageNumber.Should().Be(3);
    }

    [Fact]
    public void Constructor_ShouldSetPageSizeCorrectly()
    {
        // Arrange
        var items = new List<string> { "Item1", "Item2" };

        // Act
        var result = new PaginatedList<string>(items, 50, 1, 15);

        // Assert
        result.PageSize.Should().Be(15);
    }

    [Fact]
    public void TotalPages_ShouldCalculateCorrectly_WhenEvenlDivisible()
    {
        // Arrange
        var items = new List<string> { "Item1" };

        // Act
        var result = new PaginatedList<string>(items, 50, 1, 10);

        // Assert
        result.TotalPages.Should().Be(5);
    }

    [Fact]
    public void TotalPages_ShouldCalculateCorrectly_WhenNotEvenlyDivisible()
    {
        // Arrange
        var items = new List<string> { "Item1" };

        // Act
        var result = new PaginatedList<string>(items, 53, 1, 10);

        // Assert
        result.TotalPages.Should().Be(6);
    }

    [Fact]
    public void TotalPages_ShouldBeOne_WhenTotalCountLessThanPageSize()
    {
        // Arrange
        var items = new List<string> { "Item1", "Item2" };

        // Act
        var result = new PaginatedList<string>(items, 5, 1, 10);

        // Assert
        result.TotalPages.Should().Be(1);
    }

    [Fact]
    public void HasPreviousPage_ShouldBeFalse_WhenOnFirstPage()
    {
        // Arrange
        var items = new List<string> { "Item1" };

        // Act
        var result = new PaginatedList<string>(items, 50, 1, 10);

        // Assert
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_ShouldBeTrue_WhenNotOnFirstPage()
    {
        // Arrange
        var items = new List<string> { "Item1" };

        // Act
        var result = new PaginatedList<string>(items, 50, 2, 10);

        // Assert
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void HasNextPage_ShouldBeTrue_WhenNotOnLastPage()
    {
        // Arrange
        var items = new List<string> { "Item1" };

        // Act
        var result = new PaginatedList<string>(items, 50, 1, 10);

        // Assert
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void HasNextPage_ShouldBeFalse_WhenOnLastPage()
    {
        // Arrange
        var items = new List<string> { "Item1" };

        // Act
        var result = new PaginatedList<string>(items, 50, 5, 10);

        // Assert
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void HasNextPage_ShouldBeFalse_WhenPastLastPage()
    {
        // Arrange
        var items = new List<string> { "Item1" };

        // Act
        var result = new PaginatedList<string>(items, 50, 6, 10);

        // Assert
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void Empty_ShouldReturnEmptyList()
    {
        // Act
        var result = PaginatedList<string>.Empty;

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(0);
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void TotalPages_ShouldBeZero_WhenTotalCountIsZero()
    {
        // Arrange
        var items = new List<string>();

        // Act
        var result = new PaginatedList<string>(items, 0, 1, 10);

        // Assert
        result.TotalPages.Should().Be(0);
    }
}

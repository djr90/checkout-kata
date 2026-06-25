using AwesomeAssertions;
using CheckoutKata.UnitTests.TestData;

namespace CheckoutKata.UnitTests;

public class CheckoutTests
{
    [Fact]
    public void GetTotalPrice_WhenNoItemsScanned_ReturnsZero()
    {
        // Arrange
        var sut = new Checkout([]);

        // Act
        var total = sut.GetTotalPrice();

        // Assert
        total.Should().Be(0);
    }

    [Fact]
    public void GetTotalPrice_WhenSingleItemScanned_ReturnsItsUnitPrice()
    {
        // Arrange
        var sut = new Checkout(StandardPricing.Rules());

        // Act
        sut.Scan("A");

        // Assert
        sut.GetTotalPrice().Should().Be(50);
    }

    [Fact]
    public void GetTotalPrice_WhenDistinctItemsScanned_ReturnsSumOfUnitPrices()
    {
        // Arrange
        var sut = new Checkout(StandardPricing.Rules());

        // Act
        sut.Scan("A");
        sut.Scan("B");
        sut.Scan("C");

        // Assert
        sut.GetTotalPrice().Should().Be(100);
    }
}

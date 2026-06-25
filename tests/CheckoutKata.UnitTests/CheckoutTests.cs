using AwesomeAssertions;

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
}

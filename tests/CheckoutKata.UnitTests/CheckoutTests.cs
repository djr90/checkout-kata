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

    [Theory]
    [InlineData("AAA", 130)] // A: exactly one 3-for-130 offer
    [InlineData("AAAA", 180)] // one offer + one at unit price
    [InlineData("AAAAAA", 260)] // offer applies twice
    [InlineData("B", 30)] // B below its 2-for-45 threshold
    [InlineData("BB", 45)] // exactly one B offer
    [InlineData("BBB", 75)] // one B offer + one at unit price
    [InlineData("BAB", 95)] // order independent: two Bs (45) + one A (50)
    [InlineData("AAABBCD", 210)] // mixed: 130 + 45 + 20 + 15
    public void GetTotalPrice_AppliesMultiBuyOffersRegardlessOfOrder(string items, int expected)
    {
        // Arrange / Act
        var total = ScanAll(items).GetTotalPrice();

        // Assert
        total.Should().Be(expected);
    }

    private static Checkout ScanAll(string items)
    {
        var checkout = new Checkout(StandardPricing.Rules());
        foreach (var sku in items)
        {
            checkout.Scan(sku.ToString());
        }

        return checkout;
    }
}

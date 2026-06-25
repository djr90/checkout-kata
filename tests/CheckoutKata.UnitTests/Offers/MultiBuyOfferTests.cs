using AwesomeAssertions;
using CheckoutKata.Offers;

namespace CheckoutKata.UnitTests.Offers;

public class MultiBuyOfferTests
{
    [Theory]
    [InlineData(0, 0)] // nothing scanned
    [InlineData(1, 50)] // below threshold → unit price
    [InlineData(2, 100)] // below threshold → unit price
    [InlineData(3, 130)] // exactly one offer
    [InlineData(4, 180)] // one offer + one at unit price
    [InlineData(6, 260)] // two offers
    public void CalculatePrice_AppliesSpecialPricePerCompletedGroup(int quantity, int expected)
    {
        // Arrange
        var sut = new MultiBuyOffer(quantity: 3, specialPrice: 130);

        // Act
        var price = sut.CalculatePrice(quantity, unitPrice: 50);

        // Assert
        price.Should().Be(expected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenQuantityNotPositive_Throws(int quantity)
    {
        // Act
        Action act = () => _ = new MultiBuyOffer(quantity, specialPrice: 130);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WhenSpecialPriceNegative_Throws()
    {
        // Act
        Action act = () => _ = new MultiBuyOffer(quantity: 3, specialPrice: -1);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}

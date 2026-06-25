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

    [Fact]
    public void CalculatePrice_WhenSpecialPriceExceedsUnitTotal_CapsAtUnitTotal()
    {
        // An offer can never cost more than buying the items individually.
        var sut = new MultiBuyOffer(quantity: 3, specialPrice: 200);

        sut.CalculatePrice(quantity: 3, unitPrice: 50).Should().Be(150); // not 200
    }

    [Fact]
    public void CalculatePrice_WhenOfferTotalOverflows_Throws()
    {
        // offers × specialPrice overflows the checked int and throws rather than wrapping.
        var sut = new MultiBuyOffer(quantity: 2, specialPrice: int.MaxValue);

        Action act = () => sut.CalculatePrice(quantity: 4, unitPrice: 1);

        act.Should().Throw<OverflowException>();
    }

    [Fact]
    public void CalculatePrice_WhenUnitTotalOverflows_Throws()
    {
        // The unit-price cap (quantity × unitPrice) overflows the checked int and throws.
        // specialPrice is 0 so the offer total stays small and only the cap overflows.
        var sut = new MultiBuyOffer(quantity: 2, specialPrice: 0);

        Action act = () => sut.CalculatePrice(quantity: int.MaxValue, unitPrice: 2);

        act.Should().Throw<OverflowException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1)] // a "multi-buy" of one is just a unit price — reject it
    public void Constructor_WhenQuantityLessThanTwo_Throws(int quantity)
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

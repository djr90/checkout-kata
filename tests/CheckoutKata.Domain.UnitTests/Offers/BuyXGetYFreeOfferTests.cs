using AwesomeAssertions;
using CheckoutKata.Domain.Offers;

namespace CheckoutKata.Domain.UnitTests.Offers;

public class BuyXGetYFreeOfferTests
{
    [Theory]
    [InlineData(0, 0)] // nothing scanned
    [InlineData(1, 50)] // below the group of 3 → full price
    [InlineData(2, 100)] // still below the group → full price
    [InlineData(3, 100)] // one full group: pay for 2, one free
    [InlineData(4, 150)] // group + 1 remainder at full price
    [InlineData(5, 200)] // group + 2 remainder at full price
    [InlineData(6, 200)] // two full groups: pay for 4, two free
    public void CalculatePrice_BuyTwoGetOneFree_ChargesPaidItemsOnly(int quantity, int expected)
    {
        var sut = new BuyXGetYFreeOffer(buy: 2, free: 1);

        sut.CalculatePrice(quantity, unitPrice: 50).Should().Be(expected);
    }

    [Fact]
    public void CalculatePrice_WhenTotalOverflows_Throws()
    {
        // group of 2 (buy 1 get 1 free); paidItems × unitPrice overflows the checked int.
        var sut = new BuyXGetYFreeOffer(buy: 1, free: 1);

        Action act = () => sut.CalculatePrice(quantity: int.MaxValue, unitPrice: 2);

        act.Should().Throw<OverflowException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenBuyNotPositive_Throws(int buy)
    {
        Action act = () => _ = new BuyXGetYFreeOffer(buy, free: 1);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenFreeNotPositive_Throws(int free)
    {
        Action act = () => _ = new BuyXGetYFreeOffer(buy: 2, free);

        act.Should().Throw<ArgumentException>();
    }
}

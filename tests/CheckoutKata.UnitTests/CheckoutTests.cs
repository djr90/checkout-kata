using Ardalis.Result;
using AwesomeAssertions;
using CheckoutKata.Offers;
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

    [Fact]
    public void Scan_WhenSkuKnown_ReturnsSuccess()
    {
        // Arrange
        var sut = new Checkout(StandardPricing.Rules());

        // Act
        var result = sut.Scan("A");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Scan_WhenSkuUnknown_ReturnsNotFound()
    {
        // Arrange
        var sut = new Checkout(StandardPricing.Rules());

        // Act
        var result = sut.Scan("Z");

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public void Scan_WhenSkuUnknown_DoesNotChangeTotal()
    {
        // Arrange
        var sut = new Checkout(StandardPricing.Rules());
        sut.Scan("A");

        // Act
        sut.Scan("Z");

        // Assert
        sut.GetTotalPrice().Should().Be(50);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Scan_WhenSkuMissingOrBlank_ReturnsInvalid(string? sku)
    {
        // Arrange
        var sut = new Checkout(StandardPricing.Rules());

        // Act
        var result = sut.Scan(sku!);

        // Assert
        result.Status.Should().Be(ResultStatus.Invalid);
    }

    [Fact]
    public void Scan_WhenSkuWrongCase_ReturnsNotFound()
    {
        // Arrange — lookup is case-sensitive by design
        var sut = new Checkout(StandardPricing.Rules());

        // Act
        var result = sut.Scan("a");

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public void GetTotalPrice_WhenCalledRepeatedly_ReturnsSameValueWithoutMutating()
    {
        // Arrange
        var sut = ScanAll("AAA");

        // Act
        var first = sut.GetTotalPrice();
        var second = sut.GetTotalPrice();

        // Assert
        second.Should().Be(first).And.Be(130);
    }

    [Fact]
    public void GetTotalPrice_WhenOfferCostsMoreThanUnitPrice_StillChargesOfferPrice()
    {
        // Pins the deliberate "trust the data" stance: offers are applied blindly even
        // when the special price is worse than buying individually (risk #1 in the plan).
        var sut = new Checkout([new PricingRule("A", 50, new MultiBuyOffer(3, 200))]);

        sut.Scan("A");
        sut.Scan("A");
        sut.Scan("A");

        sut.GetTotalPrice().Should().Be(200); // not 150
    }

    [Fact]
    public void Constructor_WhenRulesNull_Throws()
    {
        // Act
        Action act = () => _ = new Checkout(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WhenDuplicateSku_Throws()
    {
        // Act
        Action act = () => _ = new Checkout([new PricingRule("A", 50), new PricingRule("A", 60)]);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void PricingRule_WhenSkuBlank_Throws(string sku)
    {
        // Act
        Action act = () => _ = new PricingRule(sku, 50);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PricingRule_WhenUnitPriceNegative_Throws()
    {
        // Act
        Action act = () => _ = new PricingRule("A", -1);

        // Assert
        act.Should().Throw<ArgumentException>();
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

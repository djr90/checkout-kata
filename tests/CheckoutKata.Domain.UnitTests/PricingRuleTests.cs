using AwesomeAssertions;

namespace CheckoutKata.Domain.UnitTests;

public class PricingRuleTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WhenSkuBlank_Throws(string sku)
    {
        // Act — the Sku value object rejects null/blank on construction.
        Action act = () => _ = new PricingRule(sku, 50);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WhenUnitPriceNegative_Throws()
    {
        // Act
        Action act = () => _ = new PricingRule("A", -1);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}

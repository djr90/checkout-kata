using AwesomeAssertions;
using CheckoutKata.Application.UnitTests.TestData;
using CheckoutKata.Domain;

namespace CheckoutKata.Application.UnitTests;

public class PricingServiceTests
{
    [Fact]
    public void Constructor_WhenRulesNull_Throws()
    {
        Action act = () => _ = new PricingService(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WhenDuplicateSku_Throws()
    {
        Action act = () =>
            _ = new PricingService([new PricingRule("A", 50), new PricingRule("A", 60)]);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("A", true)]
    [InlineData("Z", false)]
    public void HasRule_ReflectsCatalog(string sku, bool expected)
    {
        var sut = new PricingService(StandardPricing.Rules());

        sut.HasRule(sku).Should().Be(expected);
    }

    [Fact]
    public void CalculateTotal_WhenEmpty_ReturnsZero()
    {
        var sut = new PricingService(StandardPricing.Rules());

        sut.CalculateTotal(new Dictionary<Sku, int>()).Should().Be(0);
    }

    [Fact]
    public void CalculateTotal_AppliesOffersAndSumsLines()
    {
        var sut = new PricingService(StandardPricing.Rules());
        var quantities = new Dictionary<Sku, int>
        {
            ["A"] = 3, // 3-for-130
            ["B"] = 2, // 2-for-45
            ["C"] = 1, // 20
        };

        sut.CalculateTotal(quantities).Should().Be(195);
    }

    [Fact]
    public void CalculateTotal_WhenNull_Throws()
    {
        var sut = new PricingService(StandardPricing.Rules());

        Action act = () => sut.CalculateTotal(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CalculateTotal_WhenSumOfLinesOverflows_Throws()
    {
        var sut = new PricingService([
            new PricingRule("A", int.MaxValue),
            new PricingRule("B", int.MaxValue),
        ]);
        var quantities = new Dictionary<Sku, int> { ["A"] = 1, ["B"] = 1 };

        Action act = () => sut.CalculateTotal(quantities);

        act.Should().Throw<OverflowException>();
    }
}

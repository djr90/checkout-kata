using AwesomeAssertions;
using CheckoutKata.Domain;

namespace CheckoutKata.UnitTests;

public class SkuTests
{
    [Theory]
    [InlineData("a", "A")] // upper-cased
    [InlineData(" a ", "A")] // trimmed then upper-cased
    [InlineData("abc", "ABC")]
    [InlineData("A", "A")] // already normalised
    public void Constructor_NormalisesTrimAndUpperCase(string raw, string expected)
    {
        new Sku(raw).Value.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WhenNullOrBlank_Throws(string? raw)
    {
        Action act = () => _ = new Sku(raw!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equality_IgnoresCaseAndSurroundingWhitespace()
    {
        new Sku(" a ").Should().Be(new Sku("A"));
    }

    [Fact]
    public void ToString_ReturnsNormalisedValue()
    {
        new Sku(" a ").ToString().Should().Be("A");
    }
}

using Ardalis.Result;
using AwesomeAssertions;
using CheckoutKata.Offers;
using Reqnroll;

namespace CheckoutKata.AcceptanceTests.Steps;

[Binding]
public sealed class CheckoutSteps
{
    private Checkout _checkout = null!;
    private Result _lastScan = Result.Success();

    [Given("the standard pricing rules")]
    public void GivenTheStandardPricingRules()
    {
        _checkout = new Checkout([
            new PricingRule("A", 50, new MultiBuyOffer(quantity: 3, specialPrice: 130)),
            new PricingRule("B", 30, new MultiBuyOffer(quantity: 2, specialPrice: 45)),
            new PricingRule("C", 20),
            new PricingRule("D", 15),
        ]);
    }

    [When("I scan the items {string}")]
    public void WhenIScanTheItems(string items)
    {
        foreach (var sku in items)
        {
            _lastScan = _checkout.Scan(sku.ToString());
        }
    }

    [When("I scan an unknown item {string}")]
    public void WhenIScanAnUnknownItem(string sku)
    {
        _lastScan = _checkout.Scan(sku);
    }

    [Then("the total price should be {int}")]
    public void ThenTheTotalPriceShouldBe(int total)
    {
        _checkout.GetTotalPrice().Should().Be(total);
    }

    [Then("the scan result is a not-found failure")]
    public void ThenTheScanResultIsANotFoundFailure()
    {
        _lastScan.Status.Should().Be(ResultStatus.NotFound);
    }
}

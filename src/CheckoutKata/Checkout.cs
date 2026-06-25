using Ardalis.Result;

namespace CheckoutKata;

/// <inheritdoc cref="ICheckout" />
public sealed class Checkout : ICheckout
{
    public Checkout(IEnumerable<PricingRule> pricingRules) => throw new NotImplementedException();

    public Result Scan(string sku) => throw new NotImplementedException();

    public int GetTotalPrice() => throw new NotImplementedException();
}

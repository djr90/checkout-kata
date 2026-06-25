using Ardalis.Result;

namespace CheckoutKata;

/// <inheritdoc cref="ICheckout" />
public sealed class Checkout : ICheckout
{
    private readonly IReadOnlyDictionary<string, PricingRule> _rules;
    private readonly Dictionary<string, int> _counts = [];

    public Checkout(IEnumerable<PricingRule> pricingRules)
    {
        ArgumentNullException.ThrowIfNull(pricingRules);
        _rules = pricingRules.ToDictionary(rule => rule.Sku);
    }

    public Result Scan(string sku) => throw new NotImplementedException();

    public int GetTotalPrice()
    {
        var total = 0;
        foreach (var (sku, quantity) in _counts)
        {
            total += quantity * _rules[sku].UnitPrice;
        }

        return total;
    }
}

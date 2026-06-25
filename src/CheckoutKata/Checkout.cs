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

    public Result Scan(string sku)
    {
        _counts[sku] = _counts.GetValueOrDefault(sku) + 1;
        return Result.Success();
    }

    public int GetTotalPrice()
    {
        var total = 0;
        foreach (var (sku, quantity) in _counts)
        {
            var rule = _rules[sku];
            total +=
                rule.Offer?.CalculatePrice(quantity, rule.UnitPrice) ?? (quantity * rule.UnitPrice);
        }

        return total;
    }
}

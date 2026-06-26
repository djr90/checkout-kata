using CheckoutKata.Domain;

namespace CheckoutKata.Application;

/// <inheritdoc cref="IPricingService" />
public sealed class PricingService : IPricingService
{
    private readonly IReadOnlyDictionary<Sku, PricingRule> _rules;

    public PricingService(IEnumerable<PricingRule> pricingRules)
    {
        ArgumentNullException.ThrowIfNull(pricingRules);
        // ToDictionary throws ArgumentException on a duplicate SKU — a configuration error
        // we surface loudly rather than letting one rule silently win.
        _rules = pricingRules.ToDictionary(rule => rule.Sku);
    }

    public bool HasRule(Sku sku) => _rules.ContainsKey(sku);

    public int CalculateTotal(IReadOnlyDictionary<Sku, int> quantities)
    {
        ArgumentNullException.ThrowIfNull(quantities);

        var total = 0;
        foreach (var (sku, quantity) in quantities)
        {
            var rule = _rules[sku];
            var line =
                rule.Offer?.CalculatePrice(quantity, rule.UnitPrice)
                ?? checked(quantity * rule.UnitPrice);
            total = checked(total + line);
        }

        return total;
    }
}

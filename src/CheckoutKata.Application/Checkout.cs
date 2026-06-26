using Ardalis.Result;
using CheckoutKata.Domain;

namespace CheckoutKata.Application;

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
        if (string.IsNullOrWhiteSpace(sku))
        {
            return Result.Invalid(new ValidationError("A SKU must be provided."));
        }

        // SKUs are byte-exact identifiers; trim surrounding whitespace so a stray space
        // can't create a phantom miss against a rule keyed on the same (trimmed) SKU.
        var key = sku.Trim();
        if (!_rules.ContainsKey(key))
        {
            return Result.NotFound($"No pricing rule for SKU '{key}'.");
        }

        _counts[key] = _counts.GetValueOrDefault(key) + 1;
        return Result.Success();
    }

    public int GetTotalPrice()
    {
        var total = 0;
        foreach (var (sku, quantity) in _counts)
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

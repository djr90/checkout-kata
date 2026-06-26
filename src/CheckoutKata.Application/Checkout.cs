using Ardalis.Result;
using CheckoutKata.Domain;

namespace CheckoutKata.Application;

/// <inheritdoc cref="ICheckout" />
public sealed class Checkout : ICheckout
{
    private readonly IPricingService _pricing;
    private readonly Dictionary<string, int> _counts = [];

    /// <summary>
    /// Convenience constructor: prices the given rules with the default
    /// <see cref="PricingService"/>.
    /// </summary>
    public Checkout(IEnumerable<PricingRule> pricingRules)
        : this(new PricingService(pricingRules)) { }

    /// <summary>Records scanned items and delegates pricing to the supplied engine.</summary>
    public Checkout(IPricingService pricingService)
    {
        ArgumentNullException.ThrowIfNull(pricingService);
        _pricing = pricingService;
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
        if (!_pricing.HasRule(key))
        {
            return Result.NotFound($"No pricing rule for SKU '{key}'.");
        }

        _counts[key] = _counts.GetValueOrDefault(key) + 1;
        return Result.Success();
    }

    // Counting is this class's job; turning counts into money is the pricing engine's.
    public int GetTotalPrice() => _pricing.CalculateTotal(_counts);
}

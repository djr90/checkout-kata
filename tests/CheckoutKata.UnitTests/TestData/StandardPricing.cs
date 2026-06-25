namespace CheckoutKata.UnitTests.TestData;

/// <summary>
/// The canonical pricing rules from the kata README. Offers are added as the
/// implementation grows; tests assert hardcoded expected totals regardless.
/// </summary>
internal static class StandardPricing
{
    public static IReadOnlyList<PricingRule> Rules() =>
        [
            new PricingRule("A", 50),
            new PricingRule("B", 30),
            new PricingRule("C", 20),
            new PricingRule("D", 15),
        ];
}

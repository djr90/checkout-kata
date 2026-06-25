using CheckoutKata.Offers;
using CsCheck;

namespace CheckoutKata.UnitTests;

/// <summary>
/// Property-based checks over arbitrary (valid) rule sets and scan sequences.
/// Monotonic-in-quantity is intentionally NOT a property: a generous offer can make
/// more items cost less, which is the deliberate "trust the data" stance.
/// </summary>
public class CheckoutPropertyTests
{
    private static readonly string[] Skus = ["A", "B", "C", "D"];

    private static Gen<PricingRule> GenRule(string sku) =>
        from unitPrice in Gen.Int[0, 100]
        from hasOffer in Gen.Bool
        from quantity in Gen.Int[2, 5]
        from specialPrice in Gen.Int[0, 300]
        select hasOffer
            ? new PricingRule(sku, unitPrice, new MultiBuyOffer(quantity, specialPrice))
            : new PricingRule(sku, unitPrice);

    private static readonly Gen<PricingRule[]> GenRules =
        from a in GenRule("A")
        from b in GenRule("B")
        from c in GenRule("C")
        from d in GenRule("D")
        select new[] { a, b, c, d };

    private static readonly Gen<string[]> GenScans = Gen.OneOfConst(Skus).Array[0, 20];

    [Fact]
    public void GetTotalPrice_IsNeverNegative_ForAnyValidRulesAndScans()
    {
        Gen.Select(GenRules, GenScans)
            .Sample(
                (rules, scans) =>
                {
                    var checkout = new Checkout(rules);
                    foreach (var sku in scans)
                    {
                        checkout.Scan(sku);
                    }

                    return checkout.GetTotalPrice() >= 0;
                }
            );
    }

    [Fact]
    public void GetTotalPrice_IsIndependentOfScanOrder()
    {
        (
            from rules in GenRules
            from scans in GenScans
            from shuffled in Gen.Shuffle(scans)
            select (rules, scans, shuffled)
        ).Sample(t => Total(t.rules, t.scans) == Total(t.rules, t.shuffled));
    }

    private static int Total(PricingRule[] rules, string[] scans)
    {
        var checkout = new Checkout(rules);
        foreach (var sku in scans)
        {
            checkout.Scan(sku);
        }

        return checkout.GetTotalPrice();
    }
}

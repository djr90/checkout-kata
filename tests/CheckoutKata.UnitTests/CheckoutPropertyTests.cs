using CheckoutKata.Application;
using CheckoutKata.Domain;
using CheckoutKata.Domain.Offers;
using CsCheck;

namespace CheckoutKata.UnitTests;

/// <summary>
/// Property-based checks over arbitrary (valid) rule sets and scan sequences.
/// Monotonic-in-quantity is intentionally NOT a property: a generous offer can make
/// more items cost less, which is the deliberate "trust the data" stance.
/// Bounds are kept well clear of <see cref="int.MaxValue"/> so totals never overflow
/// here; the checked-overflow edge is pinned by a dedicated unit test instead.
/// </summary>
public class CheckoutPropertyTests
{
    private static readonly string[] Skus = ["A", "B", "C", "D"];

    private static Gen<PricingRule> GenRule(string sku) =>
        from unitPrice in Gen.Int[0, 10_000]
        from hasOffer in Gen.Bool
        from quantity in Gen.Int[2, 5]
        from specialPrice in Gen.Int[0, 30_000]
        select hasOffer
            ? new PricingRule(sku, unitPrice, new MultiBuyOffer(quantity, specialPrice))
            : new PricingRule(sku, unitPrice);

    private static readonly Gen<PricingRule[]> GenRules =
        from a in GenRule("A")
        from b in GenRule("B")
        from c in GenRule("C")
        from d in GenRule("D")
        select new[] { a, b, c, d };

    private static readonly Gen<string[]> GenScans = Gen.OneOfConst(Skus).Array[0, 200];

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

    // NOTE: "adding one more known item never decreases the total" is deliberately NOT a
    // property. The "never overcharge" cap floors each line at quantity × unitPrice, but a
    // generous special (e.g. 3 for 10 on a unit-50 item) makes *completing* a group cheaper
    // than the partial group, so scanning the item that completes it can lower the total.
    // This is the same "generous offer makes more items cost less" stance as monotonic-in-
    // quantity above, and an early version of this test rightly failed on that case.

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

using CheckoutKata.Domain;
using CheckoutKata.Domain.Offers;

namespace CheckoutKata.Api;

/// <summary>
/// The canonical kata catalog (A 50 / 3-for-130, B 30 / 2-for-45, C 20, D 15) used by the
/// running service. In a real system this would come from configuration or a store.
/// </summary>
internal static class SampleCatalog
{
    public static IReadOnlyList<PricingRule> Rules() =>
        [
            new PricingRule("A", 50, new MultiBuyOffer(quantity: 3, specialPrice: 130)),
            new PricingRule("B", 30, new MultiBuyOffer(quantity: 2, specialPrice: 45)),
            new PricingRule("C", 20),
            new PricingRule("D", 15),
        ];
}

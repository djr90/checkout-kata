using CheckoutKata.Domain;

namespace CheckoutKata.Application;

/// <summary>
/// Stateless pricing engine. Holds the pricing catalog and turns a set of scanned
/// quantities into a total. It is independent of any single transaction, so one
/// instance can price many baskets — concurrently and repeatably.
/// </summary>
public interface IPricingService
{
    /// <summary>True when the catalog has a pricing rule for the given SKU.</summary>
    bool HasRule(Sku sku);

    /// <summary>
    /// Total price for the given SKU → quantity map. Every key must be a known SKU
    /// (see <see cref="HasRule"/>). Order-independent; overflows throw rather than wrap.
    /// </summary>
    int CalculateTotal(IReadOnlyDictionary<Sku, int> quantities);
}

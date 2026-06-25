using CheckoutKata.Offers;

namespace CheckoutKata;

/// <summary>
/// Pricing for a single SKU: its unit price and an optional multi-buy <see cref="IOffer"/>.
/// </summary>
public sealed record PricingRule(string Sku, int UnitPrice, IOffer? Offer = null);

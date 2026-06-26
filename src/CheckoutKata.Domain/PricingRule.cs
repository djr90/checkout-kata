using Ardalis.GuardClauses;
using CheckoutKata.Domain.Offers;

namespace CheckoutKata.Domain;

/// <summary>
/// Pricing for a single SKU: its unit price and an optional multi-buy <see cref="IOffer"/>.
/// </summary>
public sealed record PricingRule(Sku Sku, int UnitPrice, IOffer? Offer = null)
{
    public int UnitPrice { get; } = Guard.Against.Negative(UnitPrice);
}

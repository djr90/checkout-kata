using Ardalis.GuardClauses;
using CheckoutKata.Domain.Offers;

namespace CheckoutKata.Domain;

/// <summary>
/// Pricing for a single SKU: its unit price and an optional multi-buy <see cref="IOffer"/>.
/// </summary>
public sealed record PricingRule(string Sku, int UnitPrice, IOffer? Offer = null)
{
    public string Sku { get; } = Guard.Against.NullOrWhiteSpace(Sku).Trim();
    public int UnitPrice { get; } = Guard.Against.Negative(UnitPrice);
}

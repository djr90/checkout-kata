namespace CheckoutKata;

/// <summary>
/// Pricing for a single SKU: its unit price.
/// </summary>
public sealed record PricingRule(string Sku, int UnitPrice);

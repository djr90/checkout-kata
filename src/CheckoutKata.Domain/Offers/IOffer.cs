namespace CheckoutKata.Domain.Offers;

/// <summary>
/// A pricing offer for a single SKU. Given how many units were scanned and the
/// SKU's unit price, returns the total price for that line.
/// </summary>
/// <remarks>
/// This seam is intentionally per-SKU. Cross-SKU promotions (e.g. "one free per ten
/// of another item") cannot see the wider basket and belong to a future basket-level
/// discount pipeline, not here.
/// </remarks>
public interface IOffer
{
    int CalculatePrice(int quantity, int unitPrice);
}

using Ardalis.GuardClauses;

namespace CheckoutKata.Offers;

/// <summary>
/// A "buy <c>quantity</c> for <c>specialPrice</c>" offer (e.g. 3 for 130). Applies as
/// many times as the scanned quantity allows; any remainder is charged at the unit price.
/// The line is capped at the plain unit-price total, so an offer can never cost the
/// customer more than buying the same items individually.
/// </summary>
public sealed class MultiBuyOffer : IOffer
{
    private readonly int _quantity;
    private readonly int _specialPrice;

    public MultiBuyOffer(int quantity, int specialPrice)
    {
        // A multi-buy needs at least two items; quantity 1 would just be a unit price.
        _quantity = Guard.Against.OutOfRange(quantity, nameof(quantity), 2, int.MaxValue);
        _specialPrice = Guard.Against.Negative(specialPrice);
    }

    public int CalculatePrice(int quantity, int unitPrice)
    {
        var offers = quantity / _quantity;
        var remainder = quantity % _quantity;
        var offerTotal = checked((offers * _specialPrice) + (remainder * unitPrice));
        return Math.Min(offerTotal, checked(quantity * unitPrice));
    }
}

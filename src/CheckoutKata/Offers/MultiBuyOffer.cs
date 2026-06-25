using Ardalis.GuardClauses;

namespace CheckoutKata.Offers;

/// <summary>
/// A "buy <c>quantity</c> for <c>specialPrice</c>" offer (e.g. 3 for 130). Applies as
/// many times as the scanned quantity allows; any remainder is charged at the unit price.
/// </summary>
public sealed class MultiBuyOffer : IOffer
{
    private readonly int _quantity;
    private readonly int _specialPrice;

    public MultiBuyOffer(int quantity, int specialPrice)
    {
        _quantity = Guard.Against.NegativeOrZero(quantity);
        _specialPrice = Guard.Against.Negative(specialPrice);
    }

    public int CalculatePrice(int quantity, int unitPrice)
    {
        var offers = quantity / _quantity;
        var remainder = quantity % _quantity;
        return (offers * _specialPrice) + (remainder * unitPrice);
    }
}

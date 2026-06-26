using Ardalis.GuardClauses;

namespace CheckoutKata.Domain.Offers;

/// <summary>
/// A "buy X, get Y free" offer (e.g. buy 2 get 1 free). For every full group of
/// (X + Y) items the customer pays for X at the unit price and the remaining Y are free;
/// items that do not complete a group are charged at the unit price (they have not yet
/// earned the free ones). Demonstrates that new offer types drop in behind <see cref="IOffer"/>
/// without touching <c>Checkout</c> or the pricing engine.
/// </summary>
public sealed class BuyXGetYFreeOffer : IOffer
{
    private readonly int _buy;
    private readonly int _free;

    public BuyXGetYFreeOffer(int buy, int free)
    {
        // Both must be positive: "buy 0" is meaningless and "get 0 free" is just a unit price.
        _buy = Guard.Against.NegativeOrZero(buy);
        _free = Guard.Against.NegativeOrZero(free);
    }

    public int CalculatePrice(int quantity, int unitPrice)
    {
        var groupSize = checked(_buy + _free);
        var groups = quantity / groupSize;
        var remainder = quantity % groupSize;

        // Paid items can never exceed quantity, so this is inherently never-overcharge.
        var paidItems = checked((groups * _buy) + remainder);
        return checked(paidItems * unitPrice);
    }
}

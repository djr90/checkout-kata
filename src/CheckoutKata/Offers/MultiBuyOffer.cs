namespace CheckoutKata.Offers;

/// <summary>
/// A "buy <c>quantity</c> for <c>specialPrice</c>" offer (e.g. 3 for 130). Applies as
/// many times as the scanned quantity allows; any remainder is charged at the unit price.
/// </summary>
public sealed class MultiBuyOffer : IOffer
{
    public MultiBuyOffer(int quantity, int specialPrice) => throw new NotImplementedException();

    public int CalculatePrice(int quantity, int unitPrice) => throw new NotImplementedException();
}

using Ardalis.Result;

namespace CheckoutKata.Application;

/// <summary>
/// A single checkout transaction. Items are scanned one at a time in any order,
/// and the running total is computed on demand from the pricing rules supplied at construction.
/// </summary>
public interface ICheckout
{
    /// <summary>
    /// Scans one unit of the given SKU.
    /// </summary>
    /// <returns>
    /// <c>Success</c> when the item was recorded;
    /// <c>NotFound</c> when the SKU has no pricing rule;
    /// <c>Invalid</c> when the SKU is null, empty or whitespace.
    /// Callers must inspect this result — an ignored failure silently drops the item.
    /// </returns>
    Result Scan(string sku);

    /// <summary>
    /// Returns the total price of everything scanned so far. Repeatable and side-effect free.
    /// </summary>
    int GetTotalPrice();
}

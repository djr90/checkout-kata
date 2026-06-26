using Ardalis.GuardClauses;

namespace CheckoutKata.Domain;

/// <summary>
/// A stock-keeping unit identifier. Guards against null/blank values and normalises at
/// construction — trims surrounding whitespace and upper-cases — so SKUs are treated as
/// case-insensitive identifiers and lookups stay consistent regardless of how a SKU was typed.
/// Trade-off: <c>ToUpperInvariant</c> can in theory collapse two distinct raw SKUs (e.g.
/// <c>"aB"</c> and <c>"Ab"</c>); accepted for this domain (see ENGINEERING-NOTES).
/// </summary>
public readonly record struct Sku
{
    public Sku(string value) =>
        Value = Guard.Against.NullOrWhiteSpace(value).Trim().ToUpperInvariant();

    public string Value { get; }

    public override string ToString() => Value;

    /// <summary>Lets a raw string flow into a <see cref="Sku"/> at call sites (catalogs, lookups).</summary>
    public static implicit operator Sku(string value) => new(value);
}

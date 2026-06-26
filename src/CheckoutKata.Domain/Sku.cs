using Ardalis.GuardClauses;

namespace CheckoutKata.Domain;

/// <summary>
/// A stock-keeping unit identifier. Guards against null/blank values and trims
/// surrounding whitespace at construction, so equality and dictionary lookups are
/// always consistent. Comparison is currently ordinal (case-sensitive).
/// </summary>
public readonly record struct Sku
{
    public Sku(string value) => Value = Guard.Against.NullOrWhiteSpace(value).Trim();

    public string Value { get; }

    public override string ToString() => Value;

    /// <summary>Lets a raw string flow into a <see cref="Sku"/> at call sites (catalogs, lookups).</summary>
    public static implicit operator Sku(string value) => new(value);
}

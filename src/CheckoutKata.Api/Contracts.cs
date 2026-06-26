namespace CheckoutKata.Api;

/// <summary>Request body for <c>POST /checkout/total</c>: a map of SKU to quantity.</summary>
public sealed record BasketRequest(IReadOnlyDictionary<string, int>? Items);

/// <summary>Response for <c>POST /checkout/total</c>.</summary>
public sealed record TotalResponse(int Total);

using CheckoutKata.Application;
using CheckoutKata.Domain;

namespace CheckoutKata.Api;

/// <summary>
/// The checkout feature: its service registration and HTTP surface. Keeps <c>Program.cs</c> to
/// composition and isolates the transport↔domain adapter logic (parse, validate, map to
/// <see cref="Sku"/>) in one testable place.
/// </summary>
internal static class CheckoutFeature
{
    public static IServiceCollection AddCheckout(this IServiceCollection services) =>
        // PricingService is stateless and thread-safe, so a singleton over a fixed catalog is ideal.
        services.AddSingleton<IPricingService>(_ => new PricingService(SampleCatalog.Rules()));

    public static IEndpointRouteBuilder MapCheckoutEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/checkout/total", PostTotal);
        return app;
    }

    private static IResult PostTotal(BasketRequest request, IPricingService pricing)
    {
        var items = request.Items ?? new Dictionary<string, int>();

        var errors = items
            .Select(item => (item.Key, Error: Validate(item.Key, item.Value, pricing)))
            .Where(x => x.Error is not null)
            .ToDictionary(x => x.Key, x => new[] { x.Error! });

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        // Entries that normalise to the same SKU (e.g. "a" and "A") are summed.
        var quantities = items
            .GroupBy(item => new Sku(item.Key), item => item.Value)
            .ToDictionary(group => group.Key, group => group.Sum());

        return Results.Ok(new TotalResponse(pricing.CalculateTotal(quantities)));
    }

    private static string? Validate(string rawSku, int quantity, IPricingService pricing) =>
        string.IsNullOrWhiteSpace(rawSku) ? "SKU must not be blank."
        : quantity < 0 ? "Quantity must not be negative."
        : !pricing.HasRule(new Sku(rawSku)) ? "No pricing rule for this SKU."
        : null;
}

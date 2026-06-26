using CheckoutKata.Api;
using CheckoutKata.Application;
using CheckoutKata.Domain;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// PricingService is stateless and thread-safe, so a singleton over a fixed catalog is ideal.
builder.Services.AddSingleton<IPricingService>(_ => new PricingService(SampleCatalog.Rules()));

var app = builder.Build();

app.MapDefaultEndpoints();

// Stateless pricing: the whole basket arrives in one request and the total comes back.
app.MapPost(
    "/checkout/total",
    (BasketRequest request, IPricingService pricing) =>
    {
        var items = request.Items ?? new Dictionary<string, int>();
        var quantities = new Dictionary<Sku, int>();
        var unknown = new List<string>();

        foreach (var (rawSku, quantity) in items)
        {
            if (quantity < 0)
            {
                return Results.BadRequest($"Quantity for '{rawSku}' must not be negative.");
            }

            if (string.IsNullOrWhiteSpace(rawSku))
            {
                unknown.Add(rawSku);
                continue;
            }

            var sku = new Sku(rawSku);
            if (!pricing.HasRule(sku))
            {
                unknown.Add(rawSku);
                continue;
            }

            quantities[sku] = quantity;
        }

        return unknown.Count > 0
            ? Results.BadRequest($"Unknown SKUs: {string.Join(", ", unknown)}.")
            : Results.Ok(new TotalResponse(pricing.CalculateTotal(quantities)));
    }
);

app.Run();

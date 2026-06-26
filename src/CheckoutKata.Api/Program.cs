using CheckoutKata.Api;
using CheckoutKata.Application;
using CheckoutKata.Domain;

var builder = WebApplication.CreateBuilder(args);

// Service defaults wire OpenTelemetry, health checks, resilience, and the shared
// ProblemDetails error contract (registered here; the matching middleware is in MapDefaultEndpoints).
builder.AddServiceDefaults();

// PricingService is stateless and thread-safe, so a singleton over a fixed catalog is ideal.
builder.Services.AddSingleton<IPricingService>(_ => new PricingService(SampleCatalog.Rules()));

var app = builder.Build();

// Health endpoints + the shared exception handler (problem+json, no leaked detail). Called before
// the app's own endpoints so the handler wraps them.
app.MapDefaultEndpoints();

// Stateless pricing: the whole basket arrives in one request and the total comes back.
// Bad input is reported as a 400 validation problem; a misconfigured catalog or an absurd
// basket that overflows surfaces as a generic 500 problem+json via the handler above.
app.MapPost(
    "/checkout/total",
    (BasketRequest request, IPricingService pricing) =>
    {
        var items = request.Items ?? new Dictionary<string, int>();
        var quantities = new Dictionary<Sku, int>();
        var errors = new Dictionary<string, string[]>();

        foreach (var (rawSku, quantity) in items)
        {
            if (string.IsNullOrWhiteSpace(rawSku))
            {
                errors[rawSku] = ["SKU must not be blank."];
            }
            else if (quantity < 0)
            {
                errors[rawSku] = ["Quantity must not be negative."];
            }
            else if (!pricing.HasRule(new Sku(rawSku)))
            {
                errors[rawSku] = ["No pricing rule for this SKU."];
            }
            else
            {
                quantities[new Sku(rawSku)] = quantity;
            }
        }

        return errors.Count > 0
            ? Results.ValidationProblem(errors)
            : Results.Ok(new TotalResponse(pricing.CalculateTotal(quantities)));
    }
);

app.Run();

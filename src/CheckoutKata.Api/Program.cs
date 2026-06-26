using CheckoutKata.Api;
using CheckoutKata.Application;
using CheckoutKata.Domain;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// RFC 9457 problem+json for every error response — including unhandled exceptions, which
// become a generic 500 with no exception detail leaked to the caller (the detail is logged
// server-side by the exception handler and carried on the trace instead).
builder.Services.AddProblemDetails();

// PricingService is stateless and thread-safe, so a singleton over a fixed catalog is ideal.
builder.Services.AddSingleton<IPricingService>(_ => new PricingService(SampleCatalog.Rules()));

var app = builder.Build();

// Registered before the endpoints so it is the innermost exception handler and wins over the
// development exception page (which would otherwise leak a stack trace).
app.UseExceptionHandler();
app.UseStatusCodePages();

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

using CheckoutKata.Api;
using CheckoutKata.Application;

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
app.MapCheckoutEndpoints();

app.Run();

using CheckoutKata.Api;
using CheckoutKata.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Service defaults wire OpenTelemetry, health checks, resilience, and the shared
// ProblemDetails error contract (registered here; the matching middleware is in MapDefaultEndpoints).
builder.AddServiceDefaults();
builder.Services.AddCheckout();

var app = builder.Build();

// Health endpoints + the shared exception handler (problem+json, no leaked detail). Called before
// the app's own endpoints so the handler wraps them.
app.MapDefaultEndpoints();

// Stateless pricing: the whole basket arrives in one request and the total comes back.
app.MapCheckoutEndpoints();

await app.RunAsync();

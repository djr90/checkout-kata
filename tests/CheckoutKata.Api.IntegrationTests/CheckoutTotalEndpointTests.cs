using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace CheckoutKata.Api.IntegrationTests;

public class CheckoutTotalEndpointTests
    : IClassFixture<CheckoutTotalEndpointTests.DevelopmentFactory>
{
    private readonly HttpClient _client;

    public CheckoutTotalEndpointTests(DevelopmentFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ValidBasket_Returns200WithTotal()
    {
        var response = await PostBasket(new { items = new { B = 2, A = 1 } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TotalResponse>();
        body!.Total.Should().Be(95);
    }

    [Fact]
    public async Task UnknownSku_Returns400ValidationProblem()
    {
        // Dictionary keys serialise verbatim (anonymous-object properties would be camel-cased).
        var response = await PostBasket(new { items = new Dictionary<string, int> { ["Z"] = 1 } });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Z").And.Contain("No pricing rule");
    }

    [Fact]
    public async Task NegativeQuantity_Returns400ValidationProblem()
    {
        var response = await PostBasket(new { items = new Dictionary<string, int> { ["A"] = -1 } });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task OverflowingBasket_Returns500ProblemJson_WithoutLeakingDetail()
    {
        // 42_949_673 × 50 (unit price of A) overflows the checked int in the pricing engine.
        var response = await PostBasket(
            new { items = new Dictionary<string, int> { ["A"] = 42_949_673 } }
        );

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var body = await response.Content.ReadAsStringAsync();
        // The caller gets a sanitised problem document — never the exception type or a stack trace.
        body.Should()
            .NotContainAny("OverflowException", "Arithmetic operation", " at CheckoutKata");
    }

    private Task<HttpResponseMessage> PostBasket(object basket) =>
        _client.PostAsJsonAsync("/checkout/total", basket);

    /// <summary>
    /// Forces the Development environment — the worst case for leaks, where the framework would
    /// otherwise serve the developer exception page.
    /// </summary>
    public sealed class DevelopmentFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder.UseEnvironment(Environments.Development);
    }
}

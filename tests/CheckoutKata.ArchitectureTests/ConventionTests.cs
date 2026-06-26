using System.Reflection;
using AwesomeAssertions;
using CheckoutKata.Application;
using CheckoutKata.Domain;
using CheckoutKata.Domain.Offers;
using NetArchTest.Rules;

namespace CheckoutKata.ArchitectureTests;

/// <summary>
/// Naming and sealing conventions. These have real teeth in a small DAG: add an
/// unsealed offer, or a service that breaks the *Service convention, and the build's
/// test gate fails.
/// </summary>
public class ConventionTests
{
    private static readonly Assembly Domain = typeof(Sku).Assembly;
    private static readonly Assembly Application = typeof(Checkout).Assembly;

    [Fact]
    public void Offers_AreSealedAndNamedOffer()
    {
        var result = Types
            .InAssembly(Domain)
            .That()
            .ImplementInterface<IOffer>()
            .Should()
            .BeSealed()
            .And()
            .HaveNameEndingWith("Offer")
            .GetResult();

        result
            .IsSuccessful.Should()
            .BeTrue("IOffer implementations must be sealed and named *Offer");
    }

    [Fact]
    public void Services_AreSealedAndNamedService()
    {
        var result = Types
            .InAssembly(Application)
            .That()
            .ImplementInterface<IPricingService>()
            .Should()
            .BeSealed()
            .And()
            .HaveNameEndingWith("Service")
            .GetResult();

        result
            .IsSuccessful.Should()
            .BeTrue("service implementations must be sealed and named *Service");
    }
}

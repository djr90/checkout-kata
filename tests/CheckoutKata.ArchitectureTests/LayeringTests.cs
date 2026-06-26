using System.Reflection;
using AwesomeAssertions;
using CheckoutKata.Application;
using CheckoutKata.Domain;
using NetArchTest.Rules;

namespace CheckoutKata.ArchitectureTests;

/// <summary>
/// Makes the clean-architecture boundary executable. The compiler already forbids a
/// Domain → Application project reference (it would be a cycle); these tests add the
/// rules the compiler can't enforce — that the domain reaches for no outer-layer or
/// application-only concern, even by type name.
/// </summary>
public class LayeringTests
{
    private static readonly Assembly Domain = typeof(Sku).Assembly;
    private static readonly Assembly Application = typeof(Checkout).Assembly;

    private const string ApplicationNamespace = "CheckoutKata.Application";

    // Outer layers, referenced by name so this test needs no project reference to them.
    private static readonly string[] OuterLayers =
    [
        "CheckoutKata.Api",
        "CheckoutKata.AppHost",
        "CheckoutKata.ServiceDefaults",
    ];

    [Fact]
    public void Domain_DoesNotDependOnApplication()
    {
        var result = Types
            .InAssembly(Domain)
            .That()
            .ResideInNamespace("CheckoutKata.Domain")
            .Should()
            .NotHaveDependencyOnAny(ApplicationNamespace)
            .GetResult();

        result
            .IsSuccessful.Should()
            .BeTrue("the domain layer must not depend on the application layer");
    }

    [Fact]
    public void Domain_DoesNotDependOnOuterLayers()
    {
        var result = Types
            .InAssembly(Domain)
            .That()
            .ResideInNamespace("CheckoutKata.Domain")
            .Should()
            .NotHaveDependencyOnAny(OuterLayers)
            .GetResult();

        result
            .IsSuccessful.Should()
            .BeTrue("the domain layer must not depend on the API / host / Aspire layers");
    }

    [Fact]
    public void Domain_DoesNotDependOnResultPattern()
    {
        // The Result pattern is an application-level contract; the pure domain stays free of it.
        var result = Types
            .InAssembly(Domain)
            .That()
            .ResideInNamespace("CheckoutKata.Domain")
            .Should()
            .NotHaveDependencyOnAny("Ardalis.Result")
            .GetResult();

        result.IsSuccessful.Should().BeTrue("the Result pattern is an application concern");
    }

    [Fact]
    public void Application_DoesNotDependOnOuterLayers()
    {
        var result = Types
            .InAssembly(Application)
            .That()
            .ResideInNamespace("CheckoutKata.Application")
            .Should()
            .NotHaveDependencyOnAny(OuterLayers)
            .GetResult();

        result
            .IsSuccessful.Should()
            .BeTrue("the application layer must not depend on the API / host / Aspire layers");
    }
}

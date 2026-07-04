using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace Koinon.ArchitectureTests;

/// <summary>
/// Enforces the clean-architecture dependency rule (ADR 0004):
/// Api → Application → Domain, with Infrastructure implementing Application
/// interfaces. A failure here means a reference was added against the arrows.
/// </summary>
public class LayerDependencyTests
{
    private static readonly System.Reflection.Assembly _domainAssembly =
        typeof(Koinon.Domain.Entities.Person).Assembly;
    private static readonly System.Reflection.Assembly _applicationAssembly =
        typeof(Koinon.Application.Interfaces.IApplicationDbContext).Assembly;
    private static readonly System.Reflection.Assembly _infrastructureAssembly =
        typeof(Koinon.Infrastructure.Data.KoinonDbContext).Assembly;

    [Fact]
    public void Domain_DependsOnNoOtherLayer()
    {
        var result = Types.InAssembly(_domainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Koinon.Application", "Koinon.Infrastructure", "Koinon.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Domain must have zero project dependencies (ADR 0004). Violations: {Names(result)}");
    }

    [Fact]
    public void Application_DoesNotDependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(_applicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Koinon.Infrastructure", "Koinon.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Application depends only on Domain (ADR 0004). Violations: {Names(result)}");
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnApi()
    {
        var result = Types.InAssembly(_infrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Koinon.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Infrastructure must not reference Api (ADR 0004). Violations: {Names(result)}");
    }

    private static string Names(TestResult result) =>
        result.FailingTypes is null
            ? "(none reported)"
            : string.Join(", ", System.Linq.Enumerable.Select(result.FailingTypes, t => t.FullName));
}

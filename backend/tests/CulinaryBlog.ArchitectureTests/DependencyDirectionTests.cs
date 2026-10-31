// Culinary Blog. Authorship is recorded in the repository history.

using System.Reflection;

namespace CulinaryBlog.ArchitectureTests;

/// <summary>Contains regression checks for dependency direction tests.</summary>
public sealed class DependencyDirectionTests
{
    /// <summary>Verifies that domain has no project or third party dependencies.</summary>
    [Fact]
    public void DomainHasNoProjectOrThirdPartyDependencies()
    {
        var disallowed = GetNonPlatformReferences(CulinaryBlog.Domain.AssemblyReference.Assembly);

        Assert.Empty(disallowed);
    }

    /// <summary>Verifies that application does not reference infrastructure or api.</summary>
    [Fact]
    public void ApplicationDoesNotReferenceInfrastructureOrApi()
    {
        var references = GetReferenceNames(CulinaryBlog.Application.AssemblyReference.Assembly);

        Assert.DoesNotContain("CulinaryBlog.Infrastructure", references);
        Assert.DoesNotContain("CulinaryBlog.Api", references);
    }

    /// <summary>Verifies that infrastructure does not reference api.</summary>
    [Fact]
    public void InfrastructureDoesNotReferenceApi()
    {
        var references = GetReferenceNames(typeof(CulinaryBlog.Infrastructure.DependencyInjection).Assembly);

        Assert.DoesNotContain("CulinaryBlog.Api", references);
    }

    private static string[] GetNonPlatformReferences(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name =>
                !name.StartsWith("System", StringComparison.Ordinal) &&
                !name.Equals("Microsoft.CSharp", StringComparison.Ordinal) &&
                !name.Equals("netstandard", StringComparison.Ordinal))
            .ToArray();

    private static string[] GetReferenceNames(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();
}

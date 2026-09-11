using System.Reflection;

namespace CulinaryBlog.ArchitectureTests;

public sealed class DependencyDirectionTests
{
    [Fact]
    public void DomainHasNoProjectOrThirdPartyDependencies()
    {
        var disallowed = GetNonPlatformReferences(CulinaryBlog.Domain.AssemblyReference.Assembly);

        Assert.Empty(disallowed);
    }

    [Fact]
    public void ApplicationDoesNotReferenceInfrastructureOrApi()
    {
        var references = GetReferenceNames(CulinaryBlog.Application.AssemblyReference.Assembly);

        Assert.DoesNotContain("CulinaryBlog.Infrastructure", references);
        Assert.DoesNotContain("CulinaryBlog.Api", references);
    }

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
                !name.StartsWith("Microsoft", StringComparison.Ordinal) &&
                !name.Equals("netstandard", StringComparison.Ordinal))
            .ToArray();

    private static string[] GetReferenceNames(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();
}

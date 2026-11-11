using System.Reflection;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using Architecture = ArchUnitNET.Domain.Architecture;

namespace CulinaryBlog.ArchitectureTests;

public sealed class DependencyDirectionTests
{
    private static readonly Assembly Domain = CulinaryBlog.Domain.AssemblyReference.Assembly;
    private static readonly Assembly Application = CulinaryBlog.Application.AssemblyReference.Assembly;
    private static readonly Assembly Infrastructure = typeof(CulinaryBlog.Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly Api = typeof(Program).Assembly;
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(Domain, Application, Infrastructure, Api)
        .Build();

    [Fact]
    public void DomainHasNoProjectOrThirdPartyDependencies()
    {
        var disallowed = GetNonPlatformReferences(CulinaryBlog.Domain.AssemblyReference.Assembly);

        Assert.Empty(disallowed);
    }

    [Fact]
    public void ApplicationDoesNotReferenceInfrastructureOrApi()
    {
        Types().That().ResideInAssembly(Application).Should()
            .NotDependOnAny(Types().That().ResideInAssembly(Infrastructure))
            .Check(Architecture);
        Types().That().ResideInAssembly(Application).Should()
            .NotDependOnAny(Types().That().ResideInAssembly(Api))
            .Check(Architecture);
    }

    [Fact]
    public void InfrastructureDoesNotReferenceApi()
    {
        Types().That().ResideInAssembly(Infrastructure).Should()
            .NotDependOnAny(Types().That().ResideInAssembly(Api))
            .Check(Architecture);
    }

    private static string[] GetNonPlatformReferences(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name =>
                !name.StartsWith("System", StringComparison.Ordinal) &&
                !name.StartsWith("Microsoft", StringComparison.Ordinal) &&
                !name.Equals("netstandard", StringComparison.Ordinal))
            .ToArray();
}

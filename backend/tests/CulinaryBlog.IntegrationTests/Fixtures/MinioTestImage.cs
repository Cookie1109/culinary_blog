// Culinary Blog. Authorship is recorded in the repository history.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Images;

namespace CulinaryBlog.IntegrationTests.Fixtures;

/// <summary>Builds and reuses the checksum-verified MinIO fixture image.</summary>
internal static class MinioTestImage
{
    /// <summary>The shared local image name used by MinIO fixtures.</summary>
    public const string Name = "culinary-blog-minio-test:2025-09-07-v2";

    private static readonly IFutureDockerImage Image = new ImageFromDockerfileBuilder()
        .WithName(Name)
        .WithDockerfileDirectory(Path.Combine(AppContext.BaseDirectory, "TestDependencies"))
        .WithDockerfile("minio.Dockerfile")
        .WithImageBuildPolicy(PullPolicy.Missing)
        .WithCleanUp(false)
        .Build();

    private static readonly Lazy<Task> Build = new(() => Image.CreateAsync());

    /// <summary>Builds the fixture image once when it is not already available.</summary>
    /// <returns>The shared image preparation task.</returns>
    public static Task PrepareAsync() => Build.Value;
}

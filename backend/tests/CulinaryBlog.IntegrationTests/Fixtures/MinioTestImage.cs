using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Images;

namespace CulinaryBlog.IntegrationTests.Fixtures;

internal static class MinioTestImage
{
    public const string Name = "culinary-blog-minio-test:2025-09-07-v2";

    private static readonly IFutureDockerImage Image = new ImageFromDockerfileBuilder()
        .WithName(Name)
        .WithDockerfileDirectory(Path.Combine(AppContext.BaseDirectory, "TestDependencies"))
        .WithDockerfile("minio.Dockerfile")
        .WithImageBuildPolicy(PullPolicy.Missing)
        .WithCleanUp(false)
        .Build();

    private static readonly Lazy<Task> Build = new(() => Image.CreateAsync());

    public static Task PrepareAsync() => Build.Value;
}

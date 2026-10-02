using System.Diagnostics.Metrics;

namespace CulinaryBlog.Infrastructure.Content;

public static class ContentMetrics
{
    public const string MeterName = "CulinaryBlog.Content";

    private static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> RecipeCreated = Meter.CreateCounter<long>("recipe.created");

    public static readonly Counter<long> RecipePublished = Meter.CreateCounter<long>("recipe.published");

    public static readonly Counter<long> RecipeUnpublished = Meter.CreateCounter<long>("recipe.unpublished");
}

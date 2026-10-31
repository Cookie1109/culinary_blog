// Culinary Blog. Authorship is recorded in the repository history.

using System.Diagnostics.Metrics;

namespace CulinaryBlog.Infrastructure.Content;

/// <summary>Represents the content metrics.</summary>
public static class ContentMetrics
{
    /// <summary>The meter name.</summary>
    public const string MeterName = "CulinaryBlog.Content";

    private static readonly Meter Meter = new(MeterName);

    /// <summary>The recipe created.</summary>
    public static readonly Counter<long> RecipeCreated = Meter.CreateCounter<long>("recipe.created");

    /// <summary>The recipe published.</summary>
    public static readonly Counter<long> RecipePublished = Meter.CreateCounter<long>("recipe.published");

    /// <summary>The recipe unpublished.</summary>
    public static readonly Counter<long> RecipeUnpublished = Meter.CreateCounter<long>("recipe.unpublished");
}

// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Common;

namespace CulinaryBlog.Domain.Recipes;

/// <summary>The image processing status.</summary>
public enum ImageProcessingStatus
{
    /// <summary>The pending value.</summary>
    Pending = 0,

    /// <summary>The ready value.</summary>
    Ready = 1,

    /// <summary>The failed value.</summary>
    Failed = 2,
}

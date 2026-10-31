// Culinary Blog. Authorship is recorded in the repository history.

using System.ComponentModel.DataAnnotations;

namespace CulinaryBlog.Infrastructure.Configuration;

/// <summary>Represents the object storage options.</summary>
public sealed class ObjectStorageOptions
{
    /// <summary>The section name.</summary>
    public const string SectionName = "ObjectStorage";

    /// <summary>Gets or initializes the endpoint.</summary>
    [Required]
    [Url]
    public string Endpoint { get; init; } = string.Empty;

    /// <summary>Gets or initializes the access key.</summary>
    [Required]
    public string AccessKey { get; init; } = string.Empty;

    /// <summary>Gets or initializes the secret key.</summary>
    [Required]
    public string SecretKey { get; init; } = string.Empty;

    /// <summary>Gets or initializes the bucket name.</summary>
    [Required]
    public string BucketName { get; init; } = string.Empty;
}

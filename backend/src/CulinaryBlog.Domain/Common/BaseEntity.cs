// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Domain.Common;

/// <summary>Represents the base entity.</summary>
public abstract class BaseEntity
{
    /// <summary>Gets or sets the resource identifier.</summary>
    public Guid Id { get; protected set; }

    /// <summary>Gets or sets the UTC creation time.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the UTC time of the last update.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>Gets or sets the created by.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>Gets or sets the updated by.</summary>
    public string? UpdatedBy { get; set; }

    /// <summary>Gets or sets a value indicating whether is deleted.</summary>
    public bool IsDeleted { get; protected set; }

    /// <summary>Gets or sets the deleted at.</summary>
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>Gets or sets the deleted by.</summary>
    public string? DeletedBy { get; set; }

    /// <summary>Gets or sets the optimistic concurrency version.</summary>
    public long Version { get; set; } = 1;

    /// <summary>Performs the soft delete operation.</summary>
    protected void SoftDelete()
    {
        if (this.IsDeleted)
        {
            return;
        }

        this.IsDeleted = true;
    }
}

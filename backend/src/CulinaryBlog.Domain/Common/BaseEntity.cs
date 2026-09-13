namespace CulinaryBlog.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; protected set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }

    public long Version { get; set; } = 1;

    protected void SoftDelete()
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
    }
}

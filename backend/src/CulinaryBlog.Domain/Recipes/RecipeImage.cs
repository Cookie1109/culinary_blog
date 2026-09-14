using CulinaryBlog.Domain.Common;

namespace CulinaryBlog.Domain.Recipes;

public enum ImageProcessingStatus
{
    Pending = 0,
    Ready = 1,
    Failed = 2,
}

public sealed class RecipeImage : BaseEntity
{
    private RecipeImage()
    {
    }

    public Guid RecipeId { get; private set; }

    public string ObjectKey { get; private set; } = string.Empty;

    public string? MediumObjectKey { get; private set; }

    public string? ThumbnailObjectKey { get; private set; }

    public string ContentType { get; private set; } = string.Empty;

    public string? AltText { get; private set; }

    public bool IsPrimary { get; private set; }

    public int OrderIndex { get; private set; }

    public ImageProcessingStatus ProcessingStatus { get; private set; }

    public static RecipeImage Create(
        Guid id,
        Guid recipeId,
        string objectKey,
        string contentType,
        string? altText,
        bool isPrimary,
        int orderIndex)
    {
        var image = new RecipeImage
        {
            Id = id,
            RecipeId = recipeId,
            ObjectKey = objectKey,
            ContentType = contentType,
            ProcessingStatus = ImageProcessingStatus.Pending,
        };
        image.UpdateMetadata(altText, isPrimary, orderIndex);
        return image;
    }

    public void UpdateMetadata(string? altText, bool isPrimary, int orderIndex)
    {
        if (altText?.Trim().Length > 200 || orderIndex < 0)
        {
            throw new DomainException("RECIPE_DATA_INVALID", "Recipe image metadata is invalid.");
        }

        AltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim();
        IsPrimary = isPrimary;
        OrderIndex = orderIndex;
    }

    public void MarkReady(string mediumObjectKey, string thumbnailObjectKey)
    {
        MediumObjectKey = mediumObjectKey;
        ThumbnailObjectKey = thumbnailObjectKey;
        ProcessingStatus = ImageProcessingStatus.Ready;
    }

    public void MarkFailed() => ProcessingStatus = ImageProcessingStatus.Failed;

    public void Delete() => SoftDelete();
}

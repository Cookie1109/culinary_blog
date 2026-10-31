// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Common;

namespace CulinaryBlog.Domain.Recipes;

/// <summary>Represents the recipe image.</summary>
public sealed class RecipeImage : BaseEntity
{
    private RecipeImage()
    {
    }

    /// <summary>Gets the recipe id.</summary>
    public Guid RecipeId { get; private set; }

    /// <summary>Gets the object key.</summary>
    public string ObjectKey { get; private set; } = string.Empty;

    /// <summary>Gets the original url.</summary>
    public string OriginalUrl { get; private set; } = string.Empty;

    /// <summary>Gets the medium url.</summary>
    public string? MediumUrl { get; private set; }

    /// <summary>Gets the thumbnail url.</summary>
    public string? ThumbnailUrl { get; private set; }

    /// <summary>Gets the medium object key.</summary>
    public string? MediumObjectKey { get; private set; }

    /// <summary>Gets the thumbnail object key.</summary>
    public string? ThumbnailObjectKey { get; private set; }

    /// <summary>Gets the content type.</summary>
    public string ContentType { get; private set; } = string.Empty;

    /// <summary>Gets the alt text.</summary>
    public string? AltText { get; private set; }

    /// <summary>Gets a value indicating whether is primary.</summary>
    public bool IsPrimary { get; private set; }

    /// <summary>Gets the order index.</summary>
    public int OrderIndex { get; private set; }

    /// <summary>Gets the processing status.</summary>
    public ImageProcessingStatus ProcessingStatus { get; private set; }

    /// <summary>Creates the recipe image.</summary>
    /// <param name="id">The identifier of the requested resource.</param>
    /// <param name="recipeId">The identifier of the recipe being accessed.</param>
    /// <param name="objectKey">The object key supplied to this operation.</param>
    /// <param name="contentType">The content type supplied to this operation.</param>
    /// <param name="altText">The alt text supplied to this operation.</param>
    /// <param name="isPrimary">The is primary supplied to this operation.</param>
    /// <param name="orderIndex">The order index supplied to this operation.</param>
    /// <returns>The create result.</returns>
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
            OriginalUrl = $"/api/v1/media/{id}/original",
            ContentType = contentType,
            ProcessingStatus = ImageProcessingStatus.Pending,
        };
        image.UpdateMetadata(altText, isPrimary, orderIndex);
        return image;
    }

    /// <summary>Updates metadata.</summary>
    /// <param name="altText">The alt text supplied to this operation.</param>
    /// <param name="isPrimary">The is primary supplied to this operation.</param>
    /// <param name="orderIndex">The order index supplied to this operation.</param>
    public void UpdateMetadata(string? altText, bool isPrimary, int orderIndex)
    {
        if (altText?.Trim().Length > 200 || orderIndex < 0)
        {
            throw new DomainException("RECIPE_DATA_INVALID", "Recipe image metadata is invalid.");
        }

        this.AltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim();
        this.IsPrimary = isPrimary;
        this.OrderIndex = orderIndex;
    }

    /// <summary>Performs the mark ready operation.</summary>
    /// <param name="mediumObjectKey">The medium object key supplied to this operation.</param>
    /// <param name="thumbnailObjectKey">The thumbnail object key supplied to this operation.</param>
    public void MarkReady(string mediumObjectKey, string thumbnailObjectKey)
    {
        this.MediumObjectKey = mediumObjectKey;
        this.ThumbnailObjectKey = thumbnailObjectKey;
        this.MediumUrl = $"/api/v1/media/{this.Id}/medium";
        this.ThumbnailUrl = $"/api/v1/media/{this.Id}/thumbnail";
        this.ProcessingStatus = ImageProcessingStatus.Ready;
    }

    /// <summary>Performs the mark pending operation.</summary>
    public void MarkPending()
    {
        this.MediumObjectKey = null;
        this.ThumbnailObjectKey = null;
        this.MediumUrl = null;
        this.ThumbnailUrl = null;
        this.ProcessingStatus = ImageProcessingStatus.Pending;
    }

    /// <summary>Performs the mark failed operation.</summary>
    public void MarkFailed() => this.ProcessingStatus = ImageProcessingStatus.Failed;

    /// <summary>Deletes the recipe image.</summary>
    public void Delete() => this.SoftDelete();
}

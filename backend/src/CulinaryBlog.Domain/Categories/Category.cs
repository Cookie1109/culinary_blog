// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Common;

namespace CulinaryBlog.Domain.Categories;

/// <summary>Represents the category.</summary>
public sealed class Category : BaseEntity
{
    private Category()
    {
    }

    /// <summary>Gets the name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the normalized name.</summary>
    public string NormalizedName { get; private set; } = string.Empty;

    /// <summary>Gets the slug.</summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>Gets the description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the image url.</summary>
    public string? ImageUrl { get; private set; }

    /// <summary>Gets the order index.</summary>
    public int OrderIndex { get; private set; }

    /// <summary>Creates the category.</summary>
    /// <param name="id">The identifier of the requested resource.</param>
    /// <param name="name">The name supplied to this operation.</param>
    /// <param name="slug">The slug supplied to this operation.</param>
    /// <param name="description">The description supplied to this operation.</param>
    /// <param name="imageUrl">The image url supplied to this operation.</param>
    /// <param name="orderIndex">The order index supplied to this operation.</param>
    /// <returns>The create result.</returns>
    public static Category Create(
        Guid id,
        string name,
        string slug,
        string? description,
        string? imageUrl,
        int orderIndex)
    {
        var category = new Category { Id = id, Slug = slug };
        category.Update(name, description, imageUrl, orderIndex);
        return category;
    }

    /// <summary>Updates the category.</summary>
    /// <param name="name">The name supplied to this operation.</param>
    /// <param name="description">The description supplied to this operation.</param>
    /// <param name="imageUrl">The image url supplied to this operation.</param>
    /// <param name="orderIndex">The order index supplied to this operation.</param>
    public void Update(string name, string? description, string? imageUrl, int orderIndex)
    {
        var trimmedName = name.Trim();
        if (trimmedName.Length is < 2 or > 50)
        {
            throw new DomainException("CATEGORY_NAME_INVALID", "Category name must contain between 2 and 50 characters.");
        }

        if (trimmedName.Contains('<') && trimmedName.IndexOf('>', trimmedName.IndexOf('<')) >= 0)
        {
            throw new DomainException("CATEGORY_NAME_INVALID", "Category name must not contain HTML.");
        }

        if (orderIndex < 0)
        {
            throw new DomainException("CATEGORY_ORDER_INVALID", "Category order must be zero or greater.");
        }

        if (description?.Trim().Length > 2000 || imageUrl?.Trim().Length > 500)
        {
            throw new DomainException("CATEGORY_DATA_INVALID", "Category description or image URL is too long.");
        }

        this.Name = trimmedName;
        this.NormalizedName = this.Name.ToUpperInvariant();
        this.Description = NormalizeOptional(description);
        this.ImageUrl = NormalizeOptional(imageUrl);
        this.OrderIndex = orderIndex;
    }

    /// <summary>Deletes the category.</summary>
    public void Delete() => this.SoftDelete();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

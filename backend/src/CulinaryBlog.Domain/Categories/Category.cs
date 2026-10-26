using CulinaryBlog.Domain.Common;

namespace CulinaryBlog.Domain.Categories;

public sealed class Category : BaseEntity
{
    private Category()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string? ImageUrl { get; private set; }

    public int OrderIndex { get; private set; }

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

        Name = trimmedName;
        NormalizedName = Name.ToUpperInvariant();
        Description = NormalizeOptional(description);
        ImageUrl = NormalizeOptional(imageUrl);
        OrderIndex = orderIndex;
    }

    public void Delete() => SoftDelete();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
